# Image

## Meta

- Name: Image
- Source: Image.xml
- Inherits: Resource
- Inheritance Chain: Image -> Resource -> RefCounted -> Object

## Brief Description

Image datatype.

## Description

Native image datatype. Contains image data which can be converted to an ImageTexture and provides commonly used *image processing* methods. The maximum width and height for an Image are MAX_WIDTH and MAX_HEIGHT. An Image cannot be assigned to a texture property of an object directly (such as Sprite2D.texture), and has to be converted manually to an ImageTexture first. **Note:** Methods that modify the image data cannot be used on VRAM-compressed images. Use decompress() to convert the image to an uncompressed format first. **Note:** The maximum image size is 16384×16384 pixels due to graphics hardware limitations. Larger images may fail to import.

## Quick Reference

```
[methods]
adjust_bcs(brightness: float, contrast: float, saturation: float) -> void
blend_rect(src: Image, src_rect: Rect2i, dst: Vector2i) -> void
blend_rect_mask(src: Image, mask: Image, src_rect: Rect2i, dst: Vector2i) -> void
blit_rect(src: Image, src_rect: Rect2i, dst: Vector2i) -> void
blit_rect_mask(src: Image, mask: Image, src_rect: Rect2i, dst: Vector2i) -> void
bump_map_to_normal_map(bump_scale: float = 1.0) -> void
clear_mipmaps() -> void
compress(mode: int (Image.CompressMode), source: int (Image.CompressSource) = 0, astc_format: int (Image.ASTCFormat) = 0) -> int (Error)
compress_from_channels(mode: int (Image.CompressMode), channels: int (Image.UsedChannels), astc_format: int (Image.ASTCFormat) = 0) -> int (Error)
compute_image_metrics(compared_image: Image, use_luma: bool) -> Dictionary
convert(format: int (Image.Format)) -> void
copy_from(src: Image) -> void
create(width: int, height: int, use_mipmaps: bool, format: int (Image.Format)) -> Image [static]
create_empty(width: int, height: int, use_mipmaps: bool, format: int (Image.Format)) -> Image [static]
create_from_data(width: int, height: int, use_mipmaps: bool, format: int (Image.Format), data: PackedByteArray) -> Image [static]
crop(width: int, height: int) -> void
decompress() -> int (Error)
detect_alpha() -> int (Image.AlphaMode) [const]
detect_used_channels(source: int (Image.CompressSource) = 0) -> int (Image.UsedChannels) [const]
fill(color: Color) -> void
fill_rect(rect: Rect2i, color: Color) -> void
fix_alpha_edges() -> void
flip_x() -> void
flip_y() -> void
generate_mipmaps(renormalize: bool = false) -> int (Error)
get_data() -> PackedByteArray [const]
get_data_size() -> int [const]
get_format() -> int (Image.Format) [const]
get_height() -> int [const]
get_mipmap_count() -> int [const]
get_mipmap_offset(mipmap: int) -> int [const]
get_pixel(x: int, y: int) -> Color [const]
get_pixelv(point: Vector2i) -> Color [const]
get_region(region: Rect2i) -> Image [const]
get_size() -> Vector2i [const]
get_used_rect() -> Rect2i [const]
get_width() -> int [const]
has_mipmaps() -> bool [const]
is_compressed() -> bool [const]
is_empty() -> bool [const]
is_invisible() -> bool [const]
linear_to_srgb() -> void
load(path: String) -> int (Error)
load_bmp_from_buffer(buffer: PackedByteArray) -> int (Error)
load_dds_from_buffer(buffer: PackedByteArray) -> int (Error)
load_exr_from_buffer(buffer: PackedByteArray) -> int (Error)
load_from_file(path: String) -> Image [static]
load_jpg_from_buffer(buffer: PackedByteArray) -> int (Error)
load_ktx_from_buffer(buffer: PackedByteArray) -> int (Error)
load_png_from_buffer(buffer: PackedByteArray) -> int (Error)
load_svg_from_buffer(buffer: PackedByteArray, scale: float = 1.0) -> int (Error)
load_svg_from_string(svg_str: String, scale: float = 1.0) -> int (Error)
load_tga_from_buffer(buffer: PackedByteArray) -> int (Error)
load_webp_from_buffer(buffer: PackedByteArray) -> int (Error)
normal_map_to_xy() -> void
premultiply_alpha() -> void
resize(width: int, height: int, interpolation: int (Image.Interpolation) = 1) -> void
resize_to_po2(square: bool = false, interpolation: int (Image.Interpolation) = 1) -> void
rgbe_to_srgb() -> Image
rotate_90(direction: int (ClockDirection)) -> void
rotate_180() -> void
save_dds(path: String) -> int (Error) [const]
save_dds_to_buffer() -> PackedByteArray [const]
save_exr(path: String, grayscale: bool = false) -> int (Error) [const]
save_exr_to_buffer(grayscale: bool = false) -> PackedByteArray [const]
save_jpg(path: String, quality: float = 0.75) -> int (Error) [const]
save_jpg_to_buffer(quality: float = 0.75) -> PackedByteArray [const]
save_png(path: String) -> int (Error) [const]
save_png_to_buffer() -> PackedByteArray [const]
save_webp(path: String, lossy: bool = false, quality: float = 0.75) -> int (Error) [const]
save_webp_to_buffer(lossy: bool = false, quality: float = 0.75) -> PackedByteArray [const]
set_data(width: int, height: int, use_mipmaps: bool, format: int (Image.Format), data: PackedByteArray) -> void
set_pixel(x: int, y: int, color: Color) -> void
set_pixelv(point: Vector2i, color: Color) -> void
shrink_x2() -> void
srgb_to_linear() -> void

[properties]
data: Dictionary = { "data": PackedByteArray(), "format": "Lum8", "height": 0, "mipmaps": false, "width": 0 }
```

## Tutorials

- [Importing images]($DOCS_URL/tutorials/assets_pipeline/importing_images.html)
- [Runtime file loading and saving]($DOCS_URL/tutorials/io/runtime_file_loading_and_saving.html)

## Methods

- adjust_bcs(brightness: float, contrast: float, saturation: float) -> void
  Adjusts this image's brightness, contrast, and saturation by the given values. Does not work if the image is compressed (see is_compressed()).

- blend_rect(src: Image, src_rect: Rect2i, dst: Vector2i) -> void
  Alpha-blends src_rect from src image to this image at coordinates dst, clipped accordingly to both image bounds. This image and src image **must** have the same format. src_rect with non-positive size is treated as empty.

- blend_rect_mask(src: Image, mask: Image, src_rect: Rect2i, dst: Vector2i) -> void
  Alpha-blends src_rect from src image to this image using mask image at coordinates dst, clipped accordingly to both image bounds. Alpha channels are required for both src and mask. dst pixels and src pixels will blend if the corresponding mask pixel's alpha value is not 0. This image and src image **must** have the same format. src image and mask image **must** have the same size (width and height) but they can have different formats. src_rect with non-positive size is treated as empty.

- blit_rect(src: Image, src_rect: Rect2i, dst: Vector2i) -> void
  Copies src_rect from src image to this image at coordinates dst, clipped accordingly to both image bounds. This image and src image **must** have the same format. src_rect with non-positive size is treated as empty. **Note:** The alpha channel data in src will overwrite the corresponding data in this image at the target position. To blend alpha channels, use blend_rect() instead.

- blit_rect_mask(src: Image, mask: Image, src_rect: Rect2i, dst: Vector2i) -> void
  Blits src_rect area from src image to this image at the coordinates given by dst, clipped accordingly to both image bounds. src pixel is copied onto dst if the corresponding mask pixel's alpha value is not 0. This image and src image **must** have the same format. src image and mask image **must** have the same size (width and height) but they can have different formats. src_rect with non-positive size is treated as empty.

- bump_map_to_normal_map(bump_scale: float = 1.0) -> void
  Converts a bump map to a normal map. A bump map provides a height offset per-pixel, while a normal map provides a normal direction per pixel.

- clear_mipmaps() -> void
  Removes the image's mipmaps.

- compress(mode: int (Image.CompressMode), source: int (Image.CompressSource) = 0, astc_format: int (Image.ASTCFormat) = 0) -> int (Error)
  Compresses the image with a VRAM-compressed format to use less memory. Can not directly access pixel data while the image is compressed. Returns error if the chosen compression mode is not available. The source parameter helps to pick the best compression method for DXT and ETC2 formats. It is ignored for ASTC compression. The astc_format parameter is only taken into account when using ASTC compression; it is ignored for all other formats. **Note:** compress() is only supported in editor builds. When run in an exported project, this method always returns ERR_UNAVAILABLE.

- compress_from_channels(mode: int (Image.CompressMode), channels: int (Image.UsedChannels), astc_format: int (Image.ASTCFormat) = 0) -> int (Error)
  Compresses the image with a VRAM-compressed format to use less memory. Can not directly access pixel data while the image is compressed. Returns error if the chosen compression mode is not available. This is an alternative to compress() that lets the user supply the channels used in order for the compressor to pick the best DXT and ETC2 formats. For other formats (non DXT or ETC2), this argument is ignored. The astc_format parameter is only taken into account when using ASTC compression; it is ignored for all other formats. **Note:** compress_from_channels() is only supported in editor builds. When run in an exported project, this method always returns ERR_UNAVAILABLE.

- compute_image_metrics(compared_image: Image, use_luma: bool) -> Dictionary
  Compute image metrics on the current image and the compared image. This can be used to calculate the similarity between two images. The dictionary contains max, mean, mean_squared, root_mean_squared and peak_snr.

- convert(format: int (Image.Format)) -> void
  Converts this image's format to the given format.

- copy_from(src: Image) -> void
  Copies src image to this image.

- create(width: int, height: int, use_mipmaps: bool, format: int (Image.Format)) -> Image [static]
  Creates an empty image of the given size and format. If use_mipmaps is true, generates mipmaps for this image (see generate_mipmaps()).

- create_empty(width: int, height: int, use_mipmaps: bool, format: int (Image.Format)) -> Image [static]
  Creates an empty image of the given size and format. If use_mipmaps is true, generates mipmaps for this image (see generate_mipmaps()).

- create_from_data(width: int, height: int, use_mipmaps: bool, format: int (Image.Format), data: PackedByteArray) -> Image [static]
  Creates a new image of the given size and format. Fills the image with the given raw data. If use_mipmaps is true, loads the mipmaps for this image from data. See generate_mipmaps().

- crop(width: int, height: int) -> void
  Crops the image to the given width and height. If the specified size is larger than the current size, the extra area is filled with black pixels.

- decompress() -> int (Error)
  Decompresses the image if it is VRAM-compressed in a supported format. This increases memory utilization, but allows modifying the image. Returns OK if the format is supported, otherwise ERR_UNAVAILABLE. All VRAM-compressed formats supported by Godot can be decompressed with this method, except FORMAT_ETC2_R11S, FORMAT_ETC2_RG11S, and FORMAT_ETC2_RGB8A1.

- detect_alpha() -> int (Image.AlphaMode) [const]
  Returns ALPHA_BLEND if the image has data for alpha values. Returns ALPHA_BIT if all the alpha values are stored in a single bit. Returns ALPHA_NONE if no data for alpha values is found.

- detect_used_channels(source: int (Image.CompressSource) = 0) -> int (Image.UsedChannels) [const]
  Returns the color channels used by this image. If the image is compressed, the original source must be specified.

- fill(color: Color) -> void
  Fills the image with color.

- fill_rect(rect: Rect2i, color: Color) -> void
  Fills rect with color.

- fix_alpha_edges() -> void
  Blends low-alpha pixels with nearby pixels.

- flip_x() -> void
  Flips the image horizontally.

- flip_y() -> void
  Flips the image vertically.

- generate_mipmaps(renormalize: bool = false) -> int (Error)
  Generates mipmaps for the image. Mipmaps are precalculated lower-resolution copies of the image that are automatically used if the image needs to be scaled down when rendered. They help improve image quality and performance when rendering. This method returns an error if the image is compressed, in a custom format, or if the image's width/height is 0. Enabling renormalize when generating mipmaps for normal map textures will make sure all resulting vector values are normalized. It is possible to check if the image has mipmaps by calling has_mipmaps() or get_mipmap_count(). Calling generate_mipmaps() on an image that already has mipmaps will replace existing mipmaps in the image.

- get_data() -> PackedByteArray [const]
  Returns a copy of the image's raw data.

- get_data_size() -> int [const]
  Returns size (in bytes) of the image's raw data.

- get_format() -> int (Image.Format) [const]
  Returns this image's format.

- get_height() -> int [const]
  Returns the image's height.

- get_mipmap_count() -> int [const]
  Returns the number of mipmap levels or 0 if the image has no mipmaps. The largest main level image is not counted as a mipmap level by this method, so if you want to include it you can add 1 to this count.

- get_mipmap_offset(mipmap: int) -> int [const]
  Returns the offset where the image's mipmap with index mipmap is stored in the data dictionary.

- get_pixel(x: int, y: int) -> Color [const]
  Returns the color of the pixel at (x, y). This is the same as get_pixelv(), but with two integer arguments instead of a Vector2i argument.

- get_pixelv(point: Vector2i) -> Color [const]
  Returns the color of the pixel at point. This is the same as get_pixel(), but with a Vector2i argument instead of two integer arguments.

- get_region(region: Rect2i) -> Image [const]
  Returns a new Image that is a copy of this Image's area specified with region.

- get_size() -> Vector2i [const]
  Returns the image's size (width and height).

- get_used_rect() -> Rect2i [const]
  Returns a Rect2i enclosing the visible portion of the image, considering each pixel with a non-zero alpha channel as visible.

- get_width() -> int [const]
  Returns the image's width.

- has_mipmaps() -> bool [const]
  Returns true if the image has generated mipmaps.

- is_compressed() -> bool [const]
  Returns true if the image is compressed.

- is_empty() -> bool [const]
  Returns true if the image has no data.

- is_invisible() -> bool [const]
  Returns true if all the image's pixels have an alpha value of 0. Returns false if any pixel has an alpha value higher than 0.

- linear_to_srgb() -> void
  Converts the entire image from linear encoding to nonlinear sRGB encoding by using a lookup table. Only works on images with FORMAT_RGB8 or FORMAT_RGBA8 formats.

- load(path: String) -> int (Error)
  Loads an image from file path. See [Supported image formats]($DOCS_URL/tutorials/assets_pipeline/importing_images.html#supported-image-formats) for a list of supported image formats and limitations. **Warning:** This method should only be used in the editor or in cases when you need to load external images at run-time, such as images located at the user:// directory, and may not work in exported projects. See also ImageTexture description for usage examples.

- load_bmp_from_buffer(buffer: PackedByteArray) -> int (Error)
  Loads an image from the binary contents of a BMP file. **Note:** Godot's BMP module doesn't support 16-bit per pixel images. Only 1-bit, 4-bit, 8-bit, 24-bit, and 32-bit per pixel images are supported. **Note:** This method is only available in engine builds with the BMP module enabled. By default, the BMP module is enabled, but it can be disabled at build-time using the module_bmp_enabled=no SCons option.

- load_dds_from_buffer(buffer: PackedByteArray) -> int (Error)
  Loads an image from the binary contents of a DDS file. **Note:** This method is only available in engine builds with the DDS module enabled. By default, the DDS module is enabled, but it can be disabled at build-time using the module_dds_enabled=no SCons option.

- load_exr_from_buffer(buffer: PackedByteArray) -> int (Error)
  Loads an image from the binary contents of an OpenEXR file.

- load_from_file(path: String) -> Image [static]
  Creates a new Image and loads data from the specified file.

- load_jpg_from_buffer(buffer: PackedByteArray) -> int (Error)
  Loads an image from the binary contents of a JPEG file.

- load_ktx_from_buffer(buffer: PackedByteArray) -> int (Error)
  Loads an image from the binary contents of a KTX(https://github.com/KhronosGroup/KTX-Software) file. Unlike most image formats, KTX can store VRAM-compressed data and embed mipmaps. **Note:** Godot's libktx implementation only supports 2D images. Cubemaps, texture arrays, and de-padding are not supported. **Note:** This method is only available in engine builds with the KTX module enabled. By default, the KTX module is enabled, but it can be disabled at build-time using the module_ktx_enabled=no SCons option.

- load_png_from_buffer(buffer: PackedByteArray) -> int (Error)
  Loads an image from the binary contents of a PNG file.

- load_svg_from_buffer(buffer: PackedByteArray, scale: float = 1.0) -> int (Error)
  Loads an image from the UTF-8 binary contents of an **uncompressed** SVG file (**.svg**). **Note:** Beware when using compressed SVG files (like **.svgz**), they need to be decompressed before loading. **Note:** This method is only available in engine builds with the SVG module enabled. By default, the SVG module is enabled, but it can be disabled at build-time using the module_svg_enabled=no SCons option.

- load_svg_from_string(svg_str: String, scale: float = 1.0) -> int (Error)
  Loads an image from the string contents of an SVG file (**.svg**). **Note:** This method is only available in engine builds with the SVG module enabled. By default, the SVG module is enabled, but it can be disabled at build-time using the module_svg_enabled=no SCons option.

- load_tga_from_buffer(buffer: PackedByteArray) -> int (Error)
  Loads an image from the binary contents of a TGA file. **Note:** This method is only available in engine builds with the TGA module enabled. By default, the TGA module is enabled, but it can be disabled at build-time using the module_tga_enabled=no SCons option.

- load_webp_from_buffer(buffer: PackedByteArray) -> int (Error)
  Loads an image from the binary contents of a WebP file.

- normal_map_to_xy() -> void
  Converts the image's data to represent coordinates on a 3D plane. This is used when the image represents a normal map. A normal map can add lots of detail to a 3D surface without increasing the polygon count.

- premultiply_alpha() -> void
  Multiplies color values with alpha values. Resulting color values for a pixel are (color * alpha)/256. See also CanvasItemMaterial.blend_mode.

- resize(width: int, height: int, interpolation: int (Image.Interpolation) = 1) -> void
  Resizes the image to the given width and height. New pixels are calculated using the interpolation mode defined via Interpolation constants.

- resize_to_po2(square: bool = false, interpolation: int (Image.Interpolation) = 1) -> void
  Resizes the image to the nearest power of 2 for the width and height. If square is true, sets width and height to be the same. New pixels are calculated using the interpolation mode defined via Interpolation constants.

- rgbe_to_srgb() -> Image
  Converts a standard linear RGBE (Red Green Blue Exponent) image to an image that uses nonlinear sRGB encoding.

- rotate_90(direction: int (ClockDirection)) -> void
  Rotates the image in the specified direction by 90 degrees. The width and height of the image must be greater than 1. If the width and height are not equal, the image will be resized.

- rotate_180() -> void
  Rotates the image by 180 degrees. The width and height of the image must be greater than 1.

- save_dds(path: String) -> int (Error) [const]
  Saves the image as a DDS (DirectDraw Surface) file to path. DDS is a container format that can store textures in various compression formats, such as DXT1, DXT5, or BC7. This function will return ERR_UNAVAILABLE if Godot was compiled without the DDS module. **Note:** The DDS module may be disabled in certain builds, which means save_dds() will return ERR_UNAVAILABLE when it is called from an exported project.

- save_dds_to_buffer() -> PackedByteArray [const]
  Saves the image as a DDS (DirectDraw Surface) file to a byte array. DDS is a container format that can store textures in various compression formats, such as DXT1, DXT5, or BC7. This function will return an empty byte array if Godot was compiled without the DDS module. **Note:** The DDS module may be disabled in certain builds, which means save_dds_to_buffer() will return an empty byte array when it is called from an exported project.

- save_exr(path: String, grayscale: bool = false) -> int (Error) [const]
  Saves the image as an EXR file to path. If grayscale is true and the image has only one channel, it will be saved explicitly as monochrome rather than one red channel. This function will return ERR_UNAVAILABLE if Godot was compiled without the TinyEXR module.

- save_exr_to_buffer(grayscale: bool = false) -> PackedByteArray [const]
  Saves the image as an EXR file to a byte array. If grayscale is true and the image has only one channel, it will be saved explicitly as monochrome rather than one red channel. This function will return an empty byte array if Godot was compiled without the TinyEXR module.

- save_jpg(path: String, quality: float = 0.75) -> int (Error) [const]
  Saves the image as a JPEG file to path with the specified quality between 0.01 and 1.0 (inclusive). Higher quality values result in better-looking output at the cost of larger file sizes. Recommended quality values are between 0.75 and 0.90. Even at quality 1.00, JPEG compression remains lossy. **Note:** JPEG does not save an alpha channel. If the Image contains an alpha channel, the image will still be saved, but the resulting JPEG file won't contain the alpha channel.

- save_jpg_to_buffer(quality: float = 0.75) -> PackedByteArray [const]
  Saves the image as a JPEG file to a byte array with the specified quality between 0.01 and 1.0 (inclusive). Higher quality values result in better-looking output at the cost of larger byte array sizes (and therefore memory usage). Recommended quality values are between 0.75 and 0.90. Even at quality 1.00, JPEG compression remains lossy. **Note:** JPEG does not save an alpha channel. If the Image contains an alpha channel, the image will still be saved, but the resulting byte array won't contain the alpha channel.

- save_png(path: String) -> int (Error) [const]
  Saves the image as a PNG file to the file at path.

- save_png_to_buffer() -> PackedByteArray [const]
  Saves the image as a PNG file to a byte array.

- save_webp(path: String, lossy: bool = false, quality: float = 0.75) -> int (Error) [const]
  Saves the image as a WebP (Web Picture) file to the file at path. By default it will save lossless. If lossy is true, the image will be saved lossy, using the quality setting between 0.0 and 1.0 (inclusive). Lossless WebP offers more efficient compression than PNG. **Note:** The WebP format is limited to a size of 16383×16383 pixels, while PNG can save larger images.

- save_webp_to_buffer(lossy: bool = false, quality: float = 0.75) -> PackedByteArray [const]
  Saves the image as a WebP (Web Picture) file to a byte array. By default it will save lossless. If lossy is true, the image will be saved lossy, using the quality setting between 0.0 and 1.0 (inclusive). Lossless WebP offers more efficient compression than PNG. **Note:** The WebP format is limited to a size of 16383×16383 pixels, while PNG can save larger images.

- set_data(width: int, height: int, use_mipmaps: bool, format: int (Image.Format), data: PackedByteArray) -> void
  Overwrites data of an existing Image. Non-static equivalent of create_from_data().

- set_pixel(x: int, y: int, color: Color) -> void
  Sets the Color of the pixel at (x, y) to color.


```
  var img_width = 10
  var img_height = 5
  var img = Image.create(img_width, img_height, false, Image.FORMAT_RGBA8)

  img.set_pixel(1, 2, Color.RED) # Sets the color at (1, 2) to red.

```

```
  int imgWidth = 10;
  int imgHeight = 5;
  var img = Image.Create(imgWidth, imgHeight, false, Image.Format.Rgba8);

  img.SetPixel(1, 2, Colors.Red); // Sets the color at (1, 2) to red.

```
  This is the same as set_pixelv(), but with a two integer arguments instead of a Vector2i argument. **Note:** Depending on the image's format, the color set here may be clamped or lose precision. Do not assume the color returned by get_pixel() to be identical to the one set here; any comparisons will likely need to use an approximation like Color.is_equal_approx(). **Note:** On grayscale image formats, only the red channel of color is used (and alpha if relevant). The green and blue channels are ignored.

- set_pixelv(point: Vector2i, color: Color) -> void
  Sets the Color of the pixel at point to color.


```
  var img_width = 10
  var img_height = 5
  var img = Image.create(img_width, img_height, false, Image.FORMAT_RGBA8)

  img.set_pixelv(Vector2i(1, 2), Color.RED) # Sets the color at (1, 2) to red.

```

```
  int imgWidth = 10;
  int imgHeight = 5;
  var img = Image.Create(imgWidth, imgHeight, false, Image.Format.Rgba8);

  img.SetPixelv(new Vector2I(1, 2), Colors.Red); // Sets the color at (1, 2) to red.

```
  This is the same as set_pixel(), but with a Vector2i argument instead of two integer arguments. **Note:** Depending on the image's format, the color set here may be clamped or lose precision. Do not assume the color returned by get_pixelv() to be identical to the one set here; any comparisons will likely need to use an approximation like Color.is_equal_approx(). **Note:** On grayscale image formats, only the red channel of color is used (and alpha if relevant). The green and blue channels are ignored.

- shrink_x2() -> void
  Shrinks the image by a factor of 2 on each axis (this divides the pixel count by 4).

- srgb_to_linear() -> void
  Converts the raw data from nonlinear sRGB encoding to linear encoding using a lookup table. Only works on images with FORMAT_RGB8 or FORMAT_RGBA8 formats. **Note:** The 8-bit formats required by this method are not suitable for storing linearly encoded values; a significant amount of color information will be lost in darker values. To maintain image quality, this method should not be used.

## Properties

- data: Dictionary = { "data": PackedByteArray(), "format": "Lum8", "height": 0, "mipmaps": false, "width": 0 } [set _set_data; get _get_data]
  Holds all the image's color data in a given format. See Format constants.

## Constants

- MAX_WIDTH = 16777216
  The maximal width allowed for Image resources.

- MAX_HEIGHT = 16777216
  The maximal height allowed for Image resources.

### Enum Format

- FORMAT_L8 = 0
  Texture format with a single 8-bit depth representing luminance.

- FORMAT_LA8 = 1
  OpenGL texture format with two values, luminance and alpha each stored with 8 bits.

- FORMAT_R8 = 2
  OpenGL texture format RED with a single component and a bitdepth of 8.

- FORMAT_RG8 = 3
  OpenGL texture format RG with two components and a bitdepth of 8 for each.

- FORMAT_RGB8 = 4
  OpenGL texture format RGB with three components, each with a bitdepth of 8. **Note:** When creating an ImageTexture, a nonlinear sRGB to linear encoding conversion is performed.

- FORMAT_RGBA8 = 5
  OpenGL texture format RGBA with four components, each with a bitdepth of 8. **Note:** When creating an ImageTexture, a nonlinear sRGB to linear encoding conversion is performed.

- FORMAT_RGBA4444 = 6
  OpenGL texture format RGBA with four components, each with a bitdepth of 4.

- FORMAT_RGB565 = 7
  OpenGL texture format RGB with three components. Red and blue have a bitdepth of 5, and green has a bitdepth of 6.

- FORMAT_RF = 8
  OpenGL texture format GL_R32F where there's one component, a 32-bit floating-point value.

- FORMAT_RGF = 9
  OpenGL texture format GL_RG32F where there are two components, each a 32-bit floating-point values.

- FORMAT_RGBF = 10
  OpenGL texture format GL_RGB32F where there are three components, each a 32-bit floating-point values.

- FORMAT_RGBAF = 11
  OpenGL texture format GL_RGBA32F where there are four components, each a 32-bit floating-point values.

- FORMAT_RH = 12
  OpenGL texture format GL_R16F where there's one component, a 16-bit "half-precision" floating-point value.

- FORMAT_RGH = 13
  OpenGL texture format GL_RG16F where there are two components, each a 16-bit "half-precision" floating-point value.

- FORMAT_RGBH = 14
  OpenGL texture format GL_RGB16F where there are three components, each a 16-bit "half-precision" floating-point value.

- FORMAT_RGBAH = 15
  OpenGL texture format GL_RGBA16F where there are four components, each a 16-bit "half-precision" floating-point value.

- FORMAT_RGBE9995 = 16
  A special OpenGL texture format where the three color components have 9 bits of precision and all three share a single 5-bit exponent.

- FORMAT_DXT1 = 17
  The S3TC(https://en.wikipedia.org/wiki/S3_Texture_Compression) texture format that uses Block Compression 1, and is the smallest variation of S3TC, only providing 1 bit of alpha and color data being premultiplied with alpha. **Note:** When creating an ImageTexture, a nonlinear sRGB to linear encoding conversion is performed.

- FORMAT_DXT3 = 18
  The S3TC(https://en.wikipedia.org/wiki/S3_Texture_Compression) texture format that uses Block Compression 2, and color data is interpreted as not having been premultiplied by alpha. Well suited for images with sharp alpha transitions between translucent and opaque areas. **Note:** When creating an ImageTexture, a nonlinear sRGB to linear encoding conversion is performed.

- FORMAT_DXT5 = 19
  The S3TC(https://en.wikipedia.org/wiki/S3_Texture_Compression) texture format also known as Block Compression 3 or BC3 that contains 64 bits of alpha channel data followed by 64 bits of DXT1-encoded color data. Color data is not premultiplied by alpha, same as DXT3. DXT5 generally produces superior results for transparent gradients compared to DXT3. **Note:** When creating an ImageTexture, a nonlinear sRGB to linear encoding conversion is performed.

- FORMAT_RGTC_R = 20
  Texture format that uses [Red Green Texture Compression](https://www.khronos.org/opengl/wiki/Red_Green_Texture_Compression), normalizing the red channel data using the same compression algorithm that DXT5 uses for the alpha channel.

- FORMAT_RGTC_RG = 21
  Texture format that uses [Red Green Texture Compression](https://www.khronos.org/opengl/wiki/Red_Green_Texture_Compression), normalizing the red and green channel data using the same compression algorithm that DXT5 uses for the alpha channel.

- FORMAT_BPTC_RGBA = 22
  Texture format that uses BPTC(https://www.khronos.org/opengl/wiki/BPTC_Texture_Compression) compression with unsigned normalized RGBA components. **Note:** When creating an ImageTexture, a nonlinear sRGB to linear encoding conversion is performed.

- FORMAT_BPTC_RGBF = 23
  Texture format that uses BPTC(https://www.khronos.org/opengl/wiki/BPTC_Texture_Compression) compression with signed floating-point RGB components.

- FORMAT_BPTC_RGBFU = 24
  Texture format that uses BPTC(https://www.khronos.org/opengl/wiki/BPTC_Texture_Compression) compression with unsigned floating-point RGB components.

- FORMAT_ETC = 25
  [Ericsson Texture Compression format 1](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC1), also referred to as "ETC1", and is part of the OpenGL ES graphics standard. This format cannot store an alpha channel.

- FORMAT_ETC2_R11 = 26
  [Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (R11_EAC variant), which provides one channel of unsigned data.

- FORMAT_ETC2_R11S = 27
  [Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (SIGNED_R11_EAC variant), which provides one channel of signed data.

- FORMAT_ETC2_RG11 = 28
  [Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (RG11_EAC variant), which provides two channels of unsigned data.

- FORMAT_ETC2_RG11S = 29
  [Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (SIGNED_RG11_EAC variant), which provides two channels of signed data.

- FORMAT_ETC2_RGB8 = 30
  [Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (RGB8 variant), which is a follow-up of ETC1 and compresses RGB888 data. **Note:** When creating an ImageTexture, a nonlinear sRGB to linear encoding conversion is performed.

- FORMAT_ETC2_RGBA8 = 31
  [Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (RGBA8variant), which compresses RGBA8888 data with full alpha support. **Note:** When creating an ImageTexture, a nonlinear sRGB to linear encoding conversion is performed.

- FORMAT_ETC2_RGB8A1 = 32
  [Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (RGB8_PUNCHTHROUGH_ALPHA1 variant), which compresses RGBA data to make alpha either fully transparent or fully opaque. **Note:** When creating an ImageTexture, a nonlinear sRGB to linear encoding conversion is performed.

- FORMAT_ETC2_RA_AS_RG = 33
  [Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (RGBA8 variant), which compresses RA data and interprets it as two channels (red and green). See also FORMAT_ETC2_RGBA8.

- FORMAT_DXT5_RA_AS_RG = 34
  The S3TC(https://en.wikipedia.org/wiki/S3_Texture_Compression) texture format also known as Block Compression 3 or BC3, which compresses RA data and interprets it as two channels (red and green). See also FORMAT_DXT5.

- FORMAT_ASTC_4x4 = 35
  [Adaptive Scalable Texture Compression](https://en.wikipedia.org/wiki/Adaptive_scalable_texture_compression). This implements the 4×4 (high quality) mode.

- FORMAT_ASTC_4x4_HDR = 36
  Same format as FORMAT_ASTC_4x4, but with the hint to let the GPU know it is used for HDR.

- FORMAT_ASTC_8x8 = 37
  [Adaptive Scalable Texture Compression](https://en.wikipedia.org/wiki/Adaptive_scalable_texture_compression). This implements the 8×8 (low quality) mode.

- FORMAT_ASTC_8x8_HDR = 38
  Same format as FORMAT_ASTC_8x8, but with the hint to let the GPU know it is used for HDR.

- FORMAT_R16 = 39
  OpenGL texture format GL_R16 where there's one component, a 16-bit unsigned normalized integer value. Since the value is normalized, each component is clamped between 0.0 and 1.0 (inclusive). **Note:** Due to limited hardware support, it is mainly recommended to be used on desktop or console devices. It may be unsupported on mobile or web, and will consequently be converted to FORMAT_RF.

- FORMAT_RG16 = 40
  OpenGL texture format GL_RG16 where there are two components, each a 16-bit unsigned normalized integer value. Since the value is normalized, each component is clamped between 0.0 and 1.0 (inclusive). **Note:** Due to limited hardware support, it is mainly recommended to be used on desktop or console devices. It may be unsupported on mobile or web, and will consequently be converted to FORMAT_RGF.

- FORMAT_RGB16 = 41
  OpenGL texture format GL_RGB16 where there are three components, each a 16-bit unsigned normalized integer value. Since the value is normalized, each component is clamped between 0.0 and 1.0 (inclusive). **Note:** Due to limited hardware support, it is mainly recommended to be used on desktop or console devices. It may be unsupported on mobile or web, and will consequently be converted to FORMAT_RGBF.

- FORMAT_RGBA16 = 42
  OpenGL texture format GL_RGBA16 where there are four components, each a 16-bit unsigned normalized integer value. Since the value is normalized, each component is clamped between 0.0 and 1.0 (inclusive). **Note:** Due to limited hardware support, it is mainly recommended to be used on desktop or console devices. It may be unsupported on mobile or web, and will consequently be converted to FORMAT_RGBAF.

- FORMAT_R16I = 43
  OpenGL texture format GL_R16UI where there's one component, a 16-bit unsigned integer value. Each component is clamped between 0 and 65535 (inclusive). **Note:** When used in a shader, the texture requires usage of usampler samplers. Additionally, it only supports nearest-neighbor filtering under the Compatibility renderer. **Note:** When sampling using Image.get_pixel(), returned Colors have to be divided by 65535 to get the correct color value.

- FORMAT_RG16I = 44
  OpenGL texture format GL_RG16UI where there are two components, each a 16-bit unsigned integer value. Each component is clamped between 0 and 65535 (inclusive). **Note:** When used in a shader, the texture requires usage of usampler samplers. Additionally, it only supports nearest-neighbor filtering under the Compatibility renderer. **Note:** When sampling using Image.get_pixel(), returned Colors have to be divided by 65535 to get the correct color value.

- FORMAT_RGB16I = 45
  OpenGL texture format GL_RGB16UI where there are three components, each a 16-bit unsigned integer value. Each component is clamped between 0 and 65535 (inclusive). **Note:** When used in a shader, the texture requires usage of usampler samplers. Additionally, it only supports nearest-neighbor filtering under the Compatibility renderer. **Note:** When sampling using Image.get_pixel(), returned Colors have to be divided by 65535 to get the correct color value.

- FORMAT_RGBA16I = 46
  OpenGL texture format GL_RGBA16UI where there are four components, each a 16-bit unsigned integer value. Each component is clamped between 0 and 65535 (inclusive). **Note:** When used in a shader, the texture requires usage of usampler samplers. Additionally, it only supports nearest-neighbor filtering under the Compatibility renderer. **Note:** When sampling using Image.get_pixel(), returned Colors have to be divided by 65535 to get the correct color value.

- FORMAT_MAX = 47
  Represents the size of the Format enum.

### Enum Interpolation

- INTERPOLATE_NEAREST = 0
  Performs nearest-neighbor interpolation. If the image is resized, it will be pixelated.

- INTERPOLATE_BILINEAR = 1
  Performs bilinear interpolation. If the image is resized, it will be blurry. This mode is faster than INTERPOLATE_CUBIC, but it results in lower quality.

- INTERPOLATE_CUBIC = 2
  Performs cubic interpolation. If the image is resized, it will be blurry. This mode often gives better results compared to INTERPOLATE_BILINEAR, at the cost of being slower.

- INTERPOLATE_TRILINEAR = 3
  Performs bilinear separately on the two most-suited mipmap levels, then linearly interpolates between them. It's slower than INTERPOLATE_BILINEAR, but produces higher-quality results with far fewer aliasing artifacts. If the image does not have mipmaps, they will be generated and used internally, but no mipmaps will be generated on the resulting image. **Note:** If you intend to scale multiple copies of the original image, it's better to call generate_mipmaps()] on it in advance, to avoid wasting processing power in generating them again and again. On the other hand, if the image already has mipmaps, they will be used, and a new set will be generated for the resulting image.

- INTERPOLATE_LANCZOS = 4
  Performs Lanczos interpolation. This is the slowest image resizing mode, but it typically gives the best results, especially when downscaling images.

### Enum AlphaMode

- ALPHA_NONE = 0
  Image is fully opaque. It does not store alpha data.

- ALPHA_BIT = 1
  Image stores either fully opaque or fully transparent pixels. Also known as punchthrough alpha.

- ALPHA_BLEND = 2
  Image stores alpha data with values varying between 0.0 and 1.0.

### Enum CompressMode

- COMPRESS_S3TC = 0
  Use S3TC compression.

- COMPRESS_ETC = 1
  Use ETC compression.

- COMPRESS_ETC2 = 2
  Use ETC2 compression.

- COMPRESS_BPTC = 3
  Use BPTC compression.

- COMPRESS_ASTC = 4
  Use ASTC compression.

- COMPRESS_MAX = 5
  Represents the size of the CompressMode enum.

### Enum UsedChannels

- USED_CHANNELS_L = 0
  The image only uses one channel for luminance (grayscale).

- USED_CHANNELS_LA = 1
  The image uses two channels for luminance and alpha, respectively.

- USED_CHANNELS_R = 2
  The image only uses the red channel.

- USED_CHANNELS_RG = 3
  The image uses two channels for red and green.

- USED_CHANNELS_RGB = 4
  The image uses three channels for red, green, and blue.

- USED_CHANNELS_RGBA = 5
  The image uses four channels for red, green, blue, and alpha.

### Enum CompressSource

- COMPRESS_SOURCE_GENERIC = 0
  Source texture (before compression) is a regular texture. Default for all textures.

- COMPRESS_SOURCE_SRGB = 1
  Source texture (before compression) uses nonlinear sRGB encoding.

- COMPRESS_SOURCE_NORMAL = 2
  Source texture (before compression) is a normal texture (e.g. it can be compressed into two channels).

### Enum ASTCFormat

- ASTC_FORMAT_4x4 = 0
  Hint to indicate that the high quality 4×4 ASTC compression format should be used.

- ASTC_FORMAT_8x8 = 1
  Hint to indicate that the low quality 8×8 ASTC compression format should be used.
