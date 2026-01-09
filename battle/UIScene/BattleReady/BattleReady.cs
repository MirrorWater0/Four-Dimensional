using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot.Collections;


public partial class BattleReady : Control
{
    public PackedScene SkillButtonScene = GD.Load<PackedScene>("res://battle/UIScene/SkillButton.tscn");
    public PackedScene PortaitScene = GD.Load<PackedScene>("res://battle/UIScene/BattleReady/PortaitFrame.tscn");
    private List<SkillButton> readyChange = new List<SkillButton>();
    Control FrameContainer => field??=GetNode("frame") as Control;
    public GridContainer Grid => field??=GetNode("GridContainer") as GridContainer;
    HBoxContainer  SkillContainer => field??=GetNode("SkillContainer") as HBoxContainer;

    PackedScene SekectButtonScene = GD.Load<PackedScene>("res://battle/UIScene/BattleReady/SelectButton.tscn");
    private PortaitFrame _dragTarget;
    public override void _Ready()
    {
        
        Initialize();
    }
    
    public async override void _Process(double delta)
    {
        if (_dragTarget != null)
        {
            _dragTarget.GlobalPosition = GetViewport().GetMousePosition() - _dragTarget.PortaitRect.Size;
        }
    }

    public void Initialize()
    {
        
        System.Collections.Generic.Dictionary<int, int> remap = new System.Collections.Generic.Dictionary<int, int>()
            { [7] = 1, [4] = 2, [1] = 3, [8] = 4, [5] = 5, [2] = 6, [9] = 7, [6] = 8, [3] = 9 };
        for (int i = 0; i < PlayerInfo.PlayerCharaters.Length; i++)
        {
            
            var portait = PortaitScene.Instantiate() as PortaitFrame;
            portait.PortaitRect.Texture = PlayerInfo.PlayerCharaters[i].Portrait;
            var positionindex = PlayerInfo.PlayerCharaters[i].PositionIndex;
            portait.Charater = PlayerInfo.PlayerCharaters[i]; //portrait获取角色引用
            Grid.GetChild(remap[positionindex]-1).AddChild(portait);
            
            
            portait.PortaitButton.Pressed += () =>{_dragTarget = portait;GD.Print("well");};
            portait.PortaitButton.ButtonUp += () => {
                _dragTarget = null;
                var olderParent = portait.GetParent();
                var newParent =  Grid.GetChildren().OfType<TextureRect>().Where(x => x.GetGlobalRect().HasPoint(GetViewport().GetMousePosition())).FirstOrDefault();
                if (newParent != null)
                {
                    if (newParent.GetChildCount() > 0)
                    {
                        var overPortait = newParent.GetChild<PortaitFrame>(0);
                        overPortait.Reparent(olderParent);
                        CreateTween().TweenProperty(overPortait,"position", new Vector2(0,0),0.2f);
                    }
                    portait.Reparent(newParent);
                    CreateTween().TweenProperty(portait,"position", new Vector2(0,0),0.1f);
                    _dragTarget = null;
                }
                else
                {
                    CreateTween().TweenProperty(portait,"position", new Vector2(0,0),0.2f);
                }
            };

        }

        //give each frame a click event to select
        for(int i = 0;i < FrameContainer.GetChildCount(); i++)
        {
            var frame = FrameContainer.GetChild<Frame>(i);
            frame.IDindex = i;
            frame.ClickButton.Visible = true;
            
            frame.ClickButton.Pressed += () =>
            {
                if(frame.Selected.Visible) return;

                frame.Selected.Visible = true;
                foreach (var other in FrameContainer.GetChildren().Where(x => x != frame).OfType<Frame>())
                {
                    other.Selected.Visible = false;
                }
                GD.Print("select",PlayerInfo.PlayerCharaters[frame.IDindex].GainedSkills.Count);
                ClearSkillContainer();

                

                var character = PlayerInfo.PlayerCharaters[frame.IDindex];
                
                for(int j = 0; j < character.GainedSkills.Count; j++)
                {
                    var selectbutton = SekectButtonScene.Instantiate() as SelectButton;
                    var skill = character.GainedSkills[j];
                    selectbutton.MySkill = skill;
                    selectbutton.ThisLabel.Text = skill.SkillName;
                    

                    int skillIndex = -1;
                    switch (skill.SkillType)
                    {
                        case Skill.SkillTypes.Attack:
                            skillIndex = 0;
                            SkillContainer.GetChild<VBoxContainer>(0).AddChild(selectbutton);
                            break;
                        case Skill.SkillTypes.Defence:
                            skillIndex = 1;
                            SkillContainer.GetChild<VBoxContainer>(1).AddChild(selectbutton);
                            break;
                        case Skill.SkillTypes.Special:
                            skillIndex = 2;
                            SkillContainer.GetChild<VBoxContainer>(2).AddChild(selectbutton);
                            break;
                    }
                    
                    if(character.TakenSkills.Contains(skill))
                    {
                        selectbutton.ButtonPressed = true;
                    }
                    
                    selectbutton.Pressed += () =>
                    {

                        PlayerInfo.PlayerCharaters[frame.IDindex].TakenSkills[skillIndex] = skill;
                        for(int i = 0; i < selectbutton.GetParent().GetChildCount(); i++)
                        {
                            SelectButton button = SkillContainer.GetChild<VBoxContainer>(skillIndex).GetChild<SelectButton>(i);
                            if(button != selectbutton) button.ButtonPressed = false;
                        }

                    };
                }
                for (int i = 0;i < SkillContainer.GetChildCount(); i++ )
                {
                    SkillContainer.GetChild<VBoxContainer>(i).QueueSort();
                }
            };

            for(int j = 0;j < frame.SkillButtonContainer.GetChildCount(); j++)
            {
                frame.SkillButtonContainer.GetChild<Button>(j).Visible = false;
            }


        }
    }


    public void ClearSkillContainer()
    {
        for (int i = 0; i < SkillContainer.GetChildCount(); i++)
        {
            for(int j = 0;j < SkillContainer.GetChild<VBoxContainer>(i).GetChildCount(); j++)
            {
                SkillContainer.GetChild<VBoxContainer>(i).GetChild<SelectButton>(j).QueueFree();
            }
        }
    }
    

    public void ComfirmTactics()
    {
        
        System.Collections.Generic.Dictionary<int, int> remap = new System.Collections.Generic.Dictionary<int,int>()
            { [1] = 7, [2] = 4, [3] = 1, [4] = 8, [5] = 5, [6] = 2, [7] = 9, [8] = 6, [9] = 3 };
        for (int i = 0; i < Grid.GetChildCount(); i++)
        {
            var texture = Grid.GetChild<TextureRect>(i);
            if (texture.GetChildCount() > 0)
            {
                GD.Print(i+1);
                texture.GetChild<PortaitFrame>(0).Charater.PositionIndex = remap[i+1];
            }
        }
        // GetTree().ChangeSceneToFile("res://battle/Battle.tscn");
    }
    
}
