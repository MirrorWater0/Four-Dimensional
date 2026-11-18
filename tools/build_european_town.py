import bpy
import math
import random
from pathlib import Path
from mathutils import Vector


random.seed(41027)
ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "asset" / "Environment" / "EuropeanTown"
OUT.mkdir(parents=True, exist_ok=True)


def hex_rgb(value):
    raw = value.lstrip("#")
    srgb = [int(raw[index:index + 2], 16) / 255.0 for index in (0, 2, 4)]
    return tuple(v / 12.92 if v < 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in srgb)


def new_collection(name):
    result = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(result)
    return result


def unlink_all(obj):
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)


def place(obj, target):
    unlink_all(obj)
    target.objects.link(obj)
    return obj


def material(name, color, roughness=0.86, noise_scale=0.0, bump_strength=0.0, variation=None):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*hex_rgb(color), 1.0)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
    shader.inputs["Base Color"].default_value = (*hex_rgb(color), 1.0)
    shader.inputs["Roughness"].default_value = roughness
    if noise_scale:
        coord = nodes.new("ShaderNodeTexCoord")
        noise = nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = noise_scale
        noise.inputs["Detail"].default_value = 4.0
        noise.inputs["Roughness"].default_value = 0.72
        links.new(coord.outputs["Generated"], noise.inputs["Vector"])
        if variation:
            ramp = nodes.new("ShaderNodeValToRGB")
            ramp.color_ramp.elements[0].position = 0.24
            ramp.color_ramp.elements[0].color = (*hex_rgb(variation[0]), 1.0)
            ramp.color_ramp.elements[1].position = 0.76
            ramp.color_ramp.elements[1].color = (*hex_rgb(variation[1]), 1.0)
            links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
            links.new(ramp.outputs["Color"], shader.inputs["Base Color"])
        bump = nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = bump_strength
        bump.inputs["Distance"].default_value = 0.075
        links.new(noise.outputs["Fac"], bump.inputs["Height"])
        links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    return mat


def mesh(name, vertices, faces, mat, target, bevel=0.0):
    data = bpy.data.meshes.new(name + " Mesh")
    data.from_pydata(vertices, [], faces)
    data.materials.append(mat)
    data.update()
    obj = bpy.data.objects.new(name, data)
    target.objects.link(obj)
    if bevel:
        mod = obj.modifiers.new("Worn stone arris", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = "ANGLE"
    return obj


def box(name, location, dimensions, mat, target, bevel=0.045, rotation=None):
    x, y, z = (value * 0.5 for value in dimensions)
    vertices = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
                (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5),
             (2, 3, 7, 6), (3, 0, 4, 7)]
    obj = mesh(name, vertices, faces, mat, target, bevel)
    obj.location = location
    uv_layer = obj.data.uv_layers.new(name="UVMap")
    axes_by_normal = {0: (1, 2), 1: (0, 2), 2: (0, 1)}
    for polygon in obj.data.polygons:
        normal_axis = max(range(3), key=lambda axis: abs(polygon.normal[axis]))
        uv_axes = axes_by_normal[normal_axis]
        for loop_index in polygon.loop_indices:
            vertex = obj.data.vertices[obj.data.loops[loop_index].vertex_index].co
            uv_layer.data[loop_index].uv = tuple((vertex[axis] + dimensions[axis] * 0.5) * 0.32 for axis in uv_axes)
    if rotation:
        obj.rotation_euler = rotation
    return obj


def cylinder(name, location, radius, depth, mat, target, vertices=24, rotation=None, bevel=0.025):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = place(bpy.context.object, target)
    obj.name = name
    if mat:
        obj.data.materials.append(mat)
    if rotation:
        obj.rotation_euler = rotation
    if bevel:
        mod = obj.modifiers.new("Softly worn edge", "BEVEL")
        mod.width = bevel
        mod.segments = 2
    return obj


def cone(name, location, radius, tip_radius, depth, mat, target, vertices=12, rotation=None):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=tip_radius, depth=depth, location=location)
    obj = place(bpy.context.object, target)
    obj.name = name
    if mat:
        obj.data.materials.append(mat)
    if rotation:
        obj.rotation_euler = rotation
    mod = obj.modifiers.new("Soft edge", "BEVEL")
    mod.width = 0.025
    mod.segments = 2
    return obj


def polyline(name, points, mat, target, radius=0.12, cyclic=False):
    data = bpy.data.curves.new(name, "CURVE")
    data.dimensions = "3D"
    data.resolution_u = 1
    data.bevel_depth = radius
    data.bevel_resolution = 2
    spline = data.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for point, co in zip(spline.points, points):
        point.co = (*co, 1.0)
    spline.use_cyclic_u = cyclic
    obj = bpy.data.objects.new(name, data)
    target.objects.link(obj)
    data.materials.append(mat)
    return obj


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for child in list(bpy.context.scene.collection.children):
    bpy.context.scene.collection.children.unlink(child)
    if child.users == 0:
        bpy.data.collections.remove(child)

ARCH = new_collection("01 Monumental Gothic City | 建筑群")
WALLS = new_collection("02 Ramparts and Ruins | 城墙废墟")
GROUND = new_collection("03 Broken Ground | 废墟地表")
DETAIL = new_collection("04 Stonework Details | 石雕细节")
VEGETATION = new_collection("05 Overgrowth | 荒草")
ATMOS = new_collection("06 Atmosphere | 大气")

MAT = {
    "stone_mid": material("Blue grey ashlar", "#747B80", noise_scale=13.0, bump_strength=0.2,
                           variation=("#5C646A", "#969A9B")),
    "stone_light": material("Weather bleached limestone", "#C0C0BB", noise_scale=10.0, bump_strength=0.16,
                             variation=("#A6AAA9", "#D7D5CE")),
    "stone_pale": material("Sunlit carved limestone", "#D5D3CB", noise_scale=9.0, bump_strength=0.12,
                            variation=("#BBBDBA", "#E4E0D7")),
    "stone_dark": material("Shadowed basalt", "#40484E", noise_scale=15.0, bump_strength=0.22,
                            variation=("#343C42", "#62686A")),
    "stone_warm": material("Ancient sandstone", "#8A8880", noise_scale=12.0, bump_strength=0.17,
                            variation=("#6C6E6C", "#A4A197")),
    "glass": material("Blackened lancet glass", "#2D383E", roughness=0.27),
    "iron": material("Oxidized iron", "#313A3D", roughness=0.45),
    "traveler_cloth": material("Wanderer | pale weather cloak", "#D7D4CA", roughness=0.92),
    "traveler_leather": material("Wanderer | dark leather", "#343B3F", roughness=0.78),
    "grass": material("Feral grass", "#59644F", roughness=0.96),
    "grass_light": material("Sun struck grass", "#939176", roughness=0.95),
    "ground": material("Dust and dark shale", "#555B5A", noise_scale=5.0, bump_strength=0.16,
                       variation=("#3E4649", "#78766D")),
}


def apply_pbr(target_material, asset_id, saturation=0.18, value=1.0, normal_strength=0.58):
    texture_dir = OUT / "textures"
    nodes = target_material.node_tree.nodes
    links = target_material.node_tree.links
    shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
    texcoord = nodes.new("ShaderNodeTexCoord")
    images = {}
    for key, suffix in (("diffuse", "diff"), ("normal", "nor_gl"), ("roughness", "rough")):
        image_path = texture_dir / f"{asset_id}_{suffix}_2k.jpg"
        image = bpy.data.images.load(str(image_path), check_existing=True)
        image.filepath = str(image_path)
        if key != "diffuse":
            image.colorspace_settings.name = "Non-Color"
        node = nodes.new("ShaderNodeTexImage")
        node.name = f"Poly Haven | {asset_id} | {key}"
        node.image = image
        node.interpolation = "Linear"
        links.new(texcoord.outputs["UV"], node.inputs["Vector"])
        images[key] = node
    tint = nodes.new("ShaderNodeHueSaturation")
    tint.inputs["Saturation"].default_value = saturation
    tint.inputs["Value"].default_value = value
    links.new(images["diffuse"].outputs["Color"], tint.inputs["Color"])
    links.new(tint.outputs["Color"], shader.inputs["Base Color"])
    links.new(images["roughness"].outputs["Color"], shader.inputs["Roughness"])
    normal = nodes.new("ShaderNodeNormalMap")
    normal.inputs["Strength"].default_value = normal_strength
    links.new(images["normal"].outputs["Color"], normal.inputs["Color"])
    bump = next(node for node in nodes if node.type == "BUMP")
    bump.inputs["Strength"].default_value = 0.12
    links.new(normal.outputs["Normal"], bump.inputs["Normal"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])


apply_pbr(MAT["stone_mid"], "old_stone_wall_02", saturation=0.15, value=1.0, normal_strength=0.5)
apply_pbr(MAT["stone_light"], "castle_brick_01", saturation=0.13, value=1.22, normal_strength=0.48)
apply_pbr(MAT["ground"], "mossy_stone_wall", saturation=0.32, value=0.68, normal_strength=0.7)


def pointed_window(name, cx, front_y, base_z, width, height, target=ARCH, stone=MAT["stone_pale"]):
    half = width * 0.5
    spring = base_z + height * 0.54
    apex = base_z + height
    left = []
    right = []
    for index in range(17):
        t = index / 16.0
        # Quadratic Gothic curves meet at a narrow apex.
        lx = cx - half * (1.0 - t)
        lz = (1 - t) ** 2 * base_z + 2 * (1 - t) * t * spring + t ** 2 * apex
        rx = cx + half * t
        rz = t ** 2 * apex + 2 * t * (1 - t) * spring + (1 - t) ** 2 * base_z
        left.append((lx, front_y, lz))
        right.append((rx, front_y, rz))
    outline = left + list(reversed(right[:-1]))
    mesh(name + " | deep opening", outline, [tuple(range(len(outline)))], MAT["glass"], target)
    polyline(name + " | pointed archivolt", outline + [outline[0]], stone, target, 0.22)
    polyline(name + " | central mullion", [(cx, front_y - 0.12, base_z + 0.2), (cx, front_y - 0.12, apex - 0.3)],
             stone, target, 0.10)
    cross_z = base_z + height * 0.5
    half_at_cross = half * (1.0 - (cross_z - base_z) / height) * 0.7
    polyline(name + " | transom", [(cx - half_at_cross, front_y - 0.12, cross_z),
                                   (cx + half_at_cross, front_y - 0.12, cross_z)], stone, target, 0.10)


def crenellated_parapet(name, cx, cy, width, depth, z, target=ARCH, material_override=None, spacing=2.4):
    stone = material_override or MAT["stone_light"]
    count_x = max(3, int(width / spacing))
    count_y = max(3, int(depth / spacing))
    for index in range(count_x + 1):
        x = cx - width * 0.5 + width * index / count_x
        for y in (cy - depth * 0.5, cy + depth * 0.5):
            box(name + " | merlon", (x, y, z + 0.85), (0.92, 0.88, 1.7), stone, target, 0.06)
    for index in range(1, count_y):
        y = cy - depth * 0.5 + depth * index / count_y
        for x in (cx - width * 0.5, cx + width * 0.5):
            box(name + " | merlon", (x, y, z + 0.85), (0.88, 0.92, 1.7), stone, target, 0.06)


def tower(name, x, y, levels, stone=MAT["stone_mid"], decorate=True, spire_height=0.0):
    # Each tuple is (base z, story height, width, depth), building an enormous stepped keep.
    for index, (base, height, width, depth) in enumerate(levels):
        cy = y
        cz = base + height * 0.5
        box(name + " | tower mass", (x, cy, cz), (width, depth, height), stone, ARCH, 0.12)
        front = y - depth * 0.5 - 0.11
        box(name + " | projecting cornice", (x, front + 0.05, base + height - 0.32), (width + 1.5, 0.72, 0.7), MAT["stone_light"], DETAIL, 0.06)
        # Heavy corner ribs emphasize height and leave the large wall planes readable.
        for side in (-1, 1):
            rib_x = x + side * (width * 0.5 - 0.55)
            box(name + " | corner rib", (rib_x, front - 0.28, cz), (1.0, 0.82, height + 0.25), MAT["stone_light"], DETAIL, 0.08)
            for z in (base + 0.75, base + height * 0.5, base + height - 0.8):
                box(name + " | rib capital", (rib_x, front - 0.34, z), (1.42, 0.95, 0.42), MAT["stone_pale"], DETAIL, 0.045)
        if decorate and height > 12:
            window_bottom = base + max(2.4, height * 0.22)
            window_top = base + height * 0.77
            window_height = min(7.2, height * 0.42)
            count = max(2, int(width / 6.0))
            for column in range(count):
                wx = x - width * 0.34 + (width * 0.68) * column / max(1, count - 1)
                pointed_window(name + " | lancet " + str(index) + "." + str(column), wx, front - 0.35,
                               window_bottom, min(2.7, width * 0.18), window_height)
            if height > 17:
                for column in range(count):
                    wx = x - width * 0.34 + (width * 0.68) * column / max(1, count - 1)
                    pointed_window(name + " | upper lancet " + str(index) + "." + str(column), wx, front - 0.35,
                                   window_top - 1.0, min(2.4, width * 0.16), window_height * 0.8)
        if index < len(levels) - 1:
            crenellated_parapet(name + " | tier battlement", x, y, width + 1.3, depth + 1.1,
                                base + height - 0.6, target=DETAIL, material_override=MAT["stone_pale"])
    base, height, width, depth = levels[-1]
    top = base + height
    if spire_height:
        # The octagonal roof grows out of the last battlement while the inner lantern stays visible.
        cylinder(name + " | spire drum", (x, y, top + 1.1), width * 0.34, 2.2, MAT["stone_pale"], ARCH, 8, bevel=0.05)
        cone(name + " | octagonal spire", (x, y, top + 1.0 + spire_height * 0.5), width * 0.48, 0.0,
             spire_height, MAT["stone_dark"], ARCH, 8, rotation=(0, 0, math.pi / 8))
        for angle in range(8):
            theta = 2 * math.pi * angle / 8
            rx = x + math.cos(theta) * width * 0.37
            ry = y + math.sin(theta) * depth * 0.37
            cone(name + " | corner pinnacle", (rx, ry, top + 2.3), 0.7, 0.0, 4.6,
                 MAT["stone_pale"], DETAIL, 8)
    else:
        crenellated_parapet(name + " | crown battlement", x, y, width + 1.2, depth + 1.2, top - 0.75,
                            target=DETAIL, material_override=MAT["stone_pale"])
        for dx in (-width * 0.34, width * 0.34):
            for dy in (-depth * 0.34, depth * 0.34):
                cone(name + " | corner pinnacle", (x + dx, y + dy, top + 2.0), 0.85, 0.0, 4.0,
                     MAT["stone_light"], DETAIL, 8)


def gothic_facade(name, x, y, width, depth, height, mat):
    box(name + " | cathedral body", (x, y, height * 0.5), (width, depth, height), mat, ARCH, 0.08)
    front = y - depth * 0.5 - 0.12
    box(name + " | lower arcade band", (x, front - 0.3, 8.0), (width + 1.0, 0.9, 1.25), MAT["stone_light"], DETAIL, 0.05)
    box(name + " | upper arcade band", (x, front - 0.3, height * 0.52), (width + 1.2, 0.9, 1.1), MAT["stone_light"], DETAIL, 0.05)
    columns = max(6, int(width / 8.0))
    for index in range(columns + 1):
        px = x - width * 0.48 + width * 0.96 * index / columns
        box(name + " | vertical buttress", (px, front - 0.42, height * 0.5), (1.15, 1.15, height + 1.0), MAT["stone_light"], DETAIL, 0.06)
        for z in (8.5, height * 0.52 + 0.55, height - 3.0):
            box(name + " | buttress cap", (px, front - 0.56, z), (1.7, 1.4, 0.55), MAT["stone_pale"], DETAIL, 0.045)
    window_columns = max(5, int(width / 8.0))
    for row, base in enumerate(range(11, int(height - 12), 18)):
        if base + 15.0 > height - 1.0:
            continue
        for index in range(window_columns):
            wx = x - width * 0.43 + width * 0.86 * (index + 0.5) / window_columns
            pointed_window(name + " | high lancet " + str(row) + "." + str(index), wx, front - 0.68,
                           base, min(5.3, width / window_columns * 0.64), 14.0)
    crenellated_parapet(name + " | roofline battlement", x, y, width + 1.4, depth + 1.1, height - 0.7,
                        target=DETAIL, material_override=MAT["stone_pale"], spacing=3.2)


def flying_buttress(name, start, end, base_z, apex_z):
    # Two sweeping masonry ribs and a short pier make the span read as a flying buttress.
    x1, y1 = start
    x2, y2 = end
    mid_x = (x1 + x2) * 0.5
    mid_y = (y1 + y2) * 0.5
    polyline(name + " | flying arch", [(x1, y1, base_z), (x1 * 0.67 + mid_x * 0.33, y1 * 0.67 + mid_y * 0.33, apex_z - 5),
                                       (mid_x, mid_y, apex_z), (x2 * 0.33 + mid_x * 0.67, y2 * 0.33 + mid_y * 0.67, apex_z - 5),
                                       (x2, y2, base_z)], MAT["stone_light"], DETAIL, 0.72)
    for px, py in (start, end):
        box(name + " | buttress pier", (px, py, base_z * 0.5), (2.3, 2.3, base_z), MAT["stone_mid"], ARCH, 0.09)
        box(name + " | pier cap", (px, py, base_z - 1.0), (3.4, 3.3, 1.1), MAT["stone_pale"], DETAIL, 0.05)


def curtain_wall(name, x, y, width, depth, height, base_z, broken=False):
    if not broken:
        box(name + " | wall curtain", (x, y, base_z + height * 0.5), (width, depth, height), MAT["stone_mid"], WALLS, 0.1)
        box(name + " | parapet walk", (x, y, base_z + height - 0.45), (width + 0.8, depth + 0.8, 0.8), MAT["stone_light"], WALLS, 0.06)
        crenellated_parapet(name, x, y, width + 0.5, depth + 0.4, base_z + height - 1.0,
                            target=WALLS, material_override=MAT["stone_pale"], spacing=3.0)
        for index in range(max(2, int(width / 9.0))):
            px = x - width * 0.44 + width * 0.88 * index / max(1, int(width / 9.0) - 1)
            box(name + " | vertical pier", (px, y - depth * 0.5 - 0.36, base_z + height * 0.48),
                (1.2, 0.8, height * 0.96), MAT["stone_light"], DETAIL, 0.07)
    else:
        count = max(4, int(width / 3.2))
        cell = width / count
        for row in range(5):
            z = base_z + row * 1.45
            for index in range(count):
                if random.random() < (0.07 if row < 3 else 0.32):
                    continue
                px = x - width * 0.5 + cell * (index + 0.5) + (cell * 0.5 if row % 2 else 0)
                top_loss = random.uniform(0.0, 2.8) if row >= 3 else 0.0
                block_h = max(0.5, 1.38 - top_loss * 0.28)
                box(name + " | fallen ashlar", (px, y + random.uniform(-0.18, 0.18), z + block_h * 0.5),
                    (cell * random.uniform(0.88, 1.08), depth, block_h),
                    random.choice((MAT["stone_dark"], MAT["stone_mid"], MAT["stone_warm"])), WALLS, 0.07,
                    rotation=(random.uniform(-0.025, 0.025), 0, random.uniform(-0.018, 0.018)))
        for index in range(3):
            px = x + random.uniform(-width * 0.42, width * 0.42)
            box(name + " | broken crown block", (px, y, base_z + 7.4 + random.uniform(-0.6, 0.6)),
                (random.uniform(1.0, 3.0), depth + 0.25, random.uniform(0.7, 1.6)), MAT["stone_light"], WALLS, 0.06,
                rotation=(random.uniform(-0.07, 0.07), random.uniform(-0.08, 0.08), random.uniform(-0.035, 0.035)))


def rubble():
    # Jagged slate, broken blocks and a pale broken stair/path pull the eye into the city.
    box("Rubble field | ground", (0, 26, -1.0), (112, 292, 2.0), MAT["ground"], GROUND, 0.0)
    for index in range(230):
        x = random.uniform(-49, 49)
        y = random.uniform(-112, 25)
        if abs(x) < 7.0 and -98 < y < 6:
            continue
        radius = random.uniform(0.25, 1.9)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1.0, location=(x, y, random.uniform(-0.2, 0.4)))
        obj = place(bpy.context.object, GROUND)
        obj.name = "Fallen stone | angular rubble"
        obj.scale = (radius * random.uniform(0.7, 1.6), radius * random.uniform(0.7, 1.4), radius * random.uniform(0.3, 0.8))
        obj.rotation_euler = (random.random(), random.random(), random.random() * math.tau)
        obj.data.materials.append(random.choice((MAT["stone_dark"], MAT["stone_mid"], MAT["stone_warm"])))
    for index in range(54):
        y = -96 + index * 1.8
        progress = index / 53.0
        x = 22.0 * (1.0 - progress) + 6.0 * math.sin(index * 0.16)
        width = random.uniform(2.8, 5.0)
        box("Pilgrim stair | broken slab", (x, y, 0.16), (width, 1.35, 0.28),
            MAT["stone_pale"] if index % 3 else MAT["stone_light"], GROUND, 0.06,
            rotation=(0, random.uniform(-0.05, 0.05), random.uniform(-0.045, 0.045)))
    for index in range(64):
        x = random.choice((-1, 1)) * random.uniform(8.0, 50.0)
        y = random.uniform(-105, -5)
        cylinder("Collapsed column drum", (x, y, random.uniform(0.25, 0.8)), random.uniform(0.35, 0.9),
                 random.uniform(0.3, 1.1), random.choice((MAT["stone_mid"], MAT["stone_warm"])), GROUND,
                 vertices=8, rotation=(random.uniform(-0.4, 0.4), random.uniform(-0.4, 0.4), random.random() * math.tau), bevel=0.04)


def overgrowth():
    for index in range(480):
        x = random.uniform(-52, 52)
        y = random.uniform(-112, 28)
        if abs(x) < 8.0 and -98 < y < 8:
            continue
        height = random.uniform(0.35, 1.65)
        lean_x = random.uniform(-0.3, 0.3)
        lean_y = random.uniform(-0.3, 0.3)
        blade = cone("Wild grass | blade", (x, y, height * 0.5), random.uniform(0.035, 0.09), 0.0, height,
                     MAT["grass"] if index % 5 else MAT["grass_light"], VEGETATION, vertices=5,
                     rotation=(lean_y, lean_x, random.uniform(0, math.tau)))
    # Clinging ivy and brush soften the broken curtain walls.
    for wall_x, wall_y, z in ((-22, -8, 4), (24, -2, 6), (-38, 24, 10), (39, 28, 12)):
        for strand in range(12):
            px = wall_x + random.uniform(-4.0, 4.0)
            top = z + random.uniform(1.0, 4.0)
            end = random.uniform(0.0, 3.0)
            points = [(px, wall_y, top), (px + random.uniform(-0.4, 0.4), wall_y - 0.25, (top + end) * 0.5),
                      (px + random.uniform(-0.6, 0.6), wall_y - 0.3, end)]
            polyline("Ivy | trailing stem", points, MAT["grass"], VEGETATION, 0.045)


def traveler():
    # A back-facing silhouette gives the surrounding ruins a readable human scale.
    x, y = 23.8, -100.0
    cloak = cone("Lone traveler | long cloak", (x, y, 1.05), 0.66, 0.27, 1.55,
                 MAT["traveler_cloth"], VEGETATION, vertices=12)
    cloak.rotation_euler.y = math.radians(3)
    for dx in (-0.17, 0.17):
        cylinder("Lone traveler | leg", (x + dx, y - 0.02, 0.48), 0.10, 0.65,
                 MAT["traveler_leather"], VEGETATION, vertices=12, bevel=0.015)
        box("Lone traveler | boot", (x + dx, y - 0.16, 0.14), (0.25, 0.48, 0.22),
            MAT["traveler_leather"], VEGETATION, 0.035)
    for side in (-1, 1):
        box("Lone traveler | sleeve", (x + side * 0.42, y, 1.42), (0.67, 0.25, 0.22),
            MAT["traveler_cloth"], VEGETATION, 0.08, rotation=(0, 0, side * -0.42))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, radius=0.22, location=(x, y, 2.03))
    head = place(bpy.context.object, VEGETATION)
    head.name = "Lone traveler | head"
    head.data.materials.append(MAT["traveler_leather"])
    for polygon in head.data.polygons:
        polygon.use_smooth = True
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, radius=0.28, location=(x, y - 0.1, 2.14))
    hood = place(bpy.context.object, VEGETATION)
    hood.name = "Lone traveler | hood"
    hood.scale = (1.1, 1.0, 0.85)
    hood.data.materials.append(MAT["traveler_cloth"])
    for polygon in hood.data.polygons:
        polygon.use_smooth = True
    cylinder("Lone traveler | walking staff", (x + 0.86, y + 0.04, 1.3), 0.045, 2.6,
             MAT["traveler_leather"], VEGETATION, vertices=10, bevel=0.01)


def build_city():
    # Far skyline: a single cathedral wall many stories high, pierced by tall lancets.
    gothic_facade("The Nameless Cathedral", 0, 108, 78, 21, 112, MAT["stone_light"])
    # Distant high-rises continue beyond the frame like a dense, vertical stone city.
    gothic_facade("Western high ward", -45, 54, 25, 19, 148, MAT["stone_mid"])
    gothic_facade("Eastern high ward", 46, 62, 27, 20, 158, MAT["stone_dark"])
    tower("High spire | north west", -25, 117, [(0, 24, 17, 18), (24, 23, 14, 15), (47, 22, 11, 12), (69, 19, 9, 10)],
          MAT["stone_mid"], True, spire_height=55)
    tower("High spire | north east", 25, 121, [(0, 24, 17, 18), (24, 24, 14, 15), (48, 21, 11, 12), (69, 20, 9, 10)],
          MAT["stone_mid"], True, spire_height=58)
    tower("High spire | central needle", 1, 137, [(0, 23, 17, 18), (23, 22, 14, 15), (45, 22, 11, 12),
                                                   (67, 20, 9, 10), (87, 19, 7, 8)],
          MAT["stone_dark"], True, spire_height=78)
    # Middle towers and arcades overlap the distant cathedral in staggered planes.
    tower("Broken keep | western tower", -34, 61, [(0, 21, 18, 19), (21, 19, 15, 16), (40, 18, 11, 12)],
          MAT["stone_dark"], True, spire_height=29)
    tower("Broken keep | eastern tower", 34, 67, [(0, 23, 19, 20), (23, 20, 15, 16), (43, 18, 11, 12)],
          MAT["stone_mid"], True, spire_height=39)
    tower("Near spire | eastern frame", 51, 25, [(0, 25, 18, 19), (25, 23, 15, 16),
                                                   (48, 22, 12, 13), (70, 19, 9, 10)],
          MAT["stone_dark"], True, spire_height=48)
    # Central monumental keep: four widening crowns form the dominant silhouette.
    tower("Citadel of the last bell", -1.5, 38, [(0, 26, 28, 26), (26, 22, 23, 22),
                                                  (48, 24, 19, 19), (72, 20, 16, 16)],
          MAT["stone_mid"], True, spire_height=0)
    pointed_window("Citadel | colossal lost gate", -1.5, 24.55, 0.0, 9.5, 17.0, stone=MAT["stone_pale"])
    for dx in (-6.2, 6.2):
        box("Citadel | gate pier", (-1.5 + dx, 24.3, 8.6), (1.25, 1.2, 17.2), MAT["stone_light"], DETAIL, 0.08)
        box("Citadel | gate capital", (-1.5 + dx, 24.15, 16.2), (2.0, 1.5, 1.0), MAT["stone_pale"], DETAIL, 0.06)
    # Crown lantern and four corner turrets above the stepped keep.
    cx, cy = -1.5, 38
    top = 92.0
    box("Citadel | open lantern", (cx, cy, top + 6.0), (8.0, 8.0, 12.0), MAT["stone_dark"], ARCH, 0.08)
    front = cy - 4.1
    for wx in (cx - 2.0, cx + 2.0):
        pointed_window("Citadel | crown opening", wx, front, top + 1.0, 1.4, 9.0)
    crenellated_parapet("Citadel | lantern crown", cx, cy, 10.0, 10.0, top + 11.0, target=DETAIL,
                        material_override=MAT["stone_pale"], spacing=1.8)
    for dx in (-6.8, 6.8):
        for dy in (-6.8, 6.8):
            tower("Citadel | corner turret", cx + dx, cy + dy,
                  [(top, 8.0, 4.8, 4.8), (top + 8.0, 5.0, 3.8, 3.8)], MAT["stone_light"], False, spire_height=12)
    # Ramparts connect the city into broad layers around the dominant keep.
    curtain_wall("Outer wall | western crown", -33, 47, 35, 4.0, 25, 10, broken=False)
    curtain_wall("Outer wall | eastern crown", 34, 50, 34, 4.0, 28, 7, broken=False)
    curtain_wall("Outer wall | west breach", -36, 9, 26, 3.0, 14, 0, broken=True)
    curtain_wall("Outer wall | east breach", 37, 17, 25, 3.0, 19, 0, broken=True)
    curtain_wall("Inner wall | western run", -29, 35, 30, 4.0, 26, 8, broken=False)
    curtain_wall("Inner wall | eastern run", 30, 39, 29, 4.0, 30, 5, broken=False)
    curtain_wall("Near ruin | left frame", -26, -25, 23, 3.0, 11, 0, broken=True)
    curtain_wall("Near ruin | right frame", 29, -18, 21, 3.0, 14, 0, broken=True)
    curtain_wall("Near ruin | broken transept", 4, -30, 21, 3.0, 7, 0, broken=True)
    # Flying buttresses arc from the middle keep to the flanking towers.
    flying_buttress("Western flying buttress", (-19, 34), (-30, 43), 25, 49)
    flying_buttress("Eastern flying buttress", (17, 37), (29, 46), 27, 55)
    flying_buttress("High western flying buttress", (-14, 45), (-27, 63), 45, 67)
    flying_buttress("High eastern flying buttress", (13, 46), (26, 66), 46, 72)
    # Ruined arcade arches at the breach, visible against the pale distance.
    for index, x in enumerate((-9.0, -2.2, 4.6, 11.4)):
        span, base, height, y = 6.0, 0.0, random.uniform(7.5, 10.0), -12.0
        points = []
        for i in range(17):
            t = i / 16.0
            px = x - span * 0.5 + t * span * 0.5
            pz = base + 2 * (1 - t) * t * height * 0.9 + t * t * height
            points.append((px, y, pz))
        for i in range(1, 17):
            t = i / 16.0
            px = x + span * 0.5 * t
            pz = (1 - t) ** 2 * height + 2 * (1 - t) * t * height * 0.9
            points.append((px, y, pz))
        polyline("Ruin arcade | pointed stone arch " + str(index + 1), points, MAT["stone_light"], WALLS, 0.5)
        for side in (-1, 1):
            px = x + side * span * 0.5
            box("Ruin arcade | pier", (px, y, height * 0.5), (0.85, 1.1, height), MAT["stone_mid"], WALLS, 0.07)
            box("Ruin arcade | capital", (px, y, height - 0.5), (1.45, 1.4, 0.65), MAT["stone_pale"], DETAIL, 0.05)


def lighting_and_camera():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("Pale winter haze") if not bpy.data.worlds else bpy.data.worlds[0]
    scene.world = world
    world.use_nodes = True
    background = next(node for node in world.node_tree.nodes if node.type == "BACKGROUND")
    background.inputs["Color"].default_value = (*hex_rgb("#D9DEE0"), 1.0)
    background.inputs["Strength"].default_value = 0.38
    bpy.ops.object.light_add(type="SUN", location=(70, -42, 120))
    sun = place(bpy.context.object, ATMOS)
    sun.name = "Cold sun | broken cloud"
    sun.rotation_euler = (math.radians(21), math.radians(-31), math.radians(-32))
    sun.data.energy = 2.8
    sun.data.angle = math.radians(9)
    bpy.ops.object.light_add(type="AREA", location=(2, -56, 72))
    fill = place(bpy.context.object, ATMOS)
    fill.name = "Sky bounce | cathedral haze"
    fill.data.energy = 5200
    fill.data.shape = "DISK"
    fill.data.size = 44
    fill.rotation_euler = (math.radians(28), 0, math.radians(8))
    bpy.ops.object.camera_add(location=(34, -132, 6.0))
    camera = place(bpy.context.object, ATMOS)
    camera.name = "Ruined Gothic city | low-angle portrait"
    look_at = Vector((-1.5, 34, 42))
    camera.rotation_euler = (look_at - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 54
    camera.data.sensor_fit = "AUTO"
    scene.camera = camera


def configure():
    scene = bpy.context.scene
    engines = [item.identifier for item in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    scene.render.resolution_x = 960
    scene.render.resolution_y = 1706
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.color_depth = "8"
    scene.render.image_settings.compression = 20
    scene.render.filepath = str(OUT / "EuropeanTown_preview.png")
    scene.render.film_transparent = False
    scene.render.use_file_extension = True
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = 0.25
    scene.view_settings.gamma = 1.0
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.render.resolution_percentage = 100
    if hasattr(scene, "eevee") and hasattr(scene.eevee, "taa_render_samples"):
        scene.eevee.taa_render_samples = 48
    scene.render.image_settings.file_format = "PNG"


rubble()
traveler()
build_city()
overgrowth()
lighting_and_camera()
configure()
bpy.context.scene.render.filepath = str(OUT / "EuropeanTown_preview.png")
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "EuropeanTown.blend"))
bpy.ops.render.render(write_still=True)
print("Saved scene:", OUT / "EuropeanTown.blend")
print("Saved preview:", OUT / "EuropeanTown_preview.png")
print("Objects:", len(bpy.context.scene.objects), "Collections:", len(bpy.context.scene.collection.children))
