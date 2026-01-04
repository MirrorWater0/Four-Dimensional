using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot.Collections;


public partial class BattleReady : Control
{
    public PackedScene SkillButtonScene = GD.Load<PackedScene>("res://battle/UIScene/skill.tscn");
    public PackedScene PortaitScene = GD.Load<PackedScene>("res://battle/UIScene/BattleReady/PortaitFrame.tscn");
    private List<SkillButton> readyChange = new List<SkillButton>();
    Control FrameContainer => field??=GetNode("frame") as Control;
    public GridContainer Grid => field??=GetNode("GridContainer") as GridContainer;
    HBoxContainer UntakeContainer => field??=GetNode("HBoxContainer") as HBoxContainer;
    
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
        for (int i = 0; i < FrameContainer.GetChildCount(); i++)
        {
            var frame = FrameContainer.GetChild<Frame>(i);
            //text
            frame.NameLabel.Text = PlayerInfo.PlayerCharaters[i].CharaterName;
            frame.Power.Text = PlayerInfo.PlayerCharaters[i].Power.ToString();
            frame.Defence.Text = PlayerInfo.PlayerCharaters[i].Survivability.ToString();
            
            for (int j = 0; j < frame.SkillButtonContainer.GetChildCount(); j++)
            {
                var button = frame.SkillButtonContainer.GetChild<SkillButton>(j);
                button.Modulate = new Color(0.9f, 0.9f, 0.9f, 1f);
                button.SelfSkill = PlayerInfo.PlayerCharaters[i].Skills[j];
                button.NameLabel.Text = button.SelfSkill.SkillName;
                button.Pressed += () => 
                { 
                    readyChange.Add(button);
                    button.Modulate = new Color(1, 1, 1, 1);
                    AwaitChose(frame.GetIndex()); 
                };
            }
        }
        
        for (int i = 0; i < UntakeContainer.GetChildCount(); i++)
        {
            var untake = UntakeContainer.GetChild(i);
            for (int j = 0; j < PlayerInfo.PlayerCharaters[i].UntakeSkills.Count; j++)
            {
                var untakeButton = SkillButtonScene.Instantiate() as SkillButton;
                
                untakeButton.SelfSkill = PlayerInfo.PlayerCharaters[i].UntakeSkills[j];
                untakeButton.NameLabel.Text = untakeButton.SelfSkill.SkillName;
                untakeButton.Modulate = new Color(0.9f, 0.9f, 0.9f, 0.9f);
                untake.AddChild(untakeButton);
                
                untakeButton.Position = j * 50 * Vector2.Down;
                untakeButton.PositionIndex = untakeButton.Position;
                
                untakeButton.Pressed += () =>
                {
                    readyChange.Add(untakeButton);
                    untakeButton.Modulate = new Color(1, 1, 1, 1);
                    AwaitChose(untake.GetIndex()); 
                };
            }
        }

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
    }


    public void ChangePosition(SkillButton skillButton1, SkillButton skillButton2)
    {
        var tempParent = skillButton1.GetParent();
        skillButton1.Reparent(skillButton2.GetParent());
        skillButton2.Reparent(tempParent);
        
        var temp = skillButton1.PositionIndex;
        skillButton1.PositionIndex = skillButton2.PositionIndex;
        skillButton2.PositionIndex = temp;
        CreateTween().TweenProperty( skillButton1,"position", skillButton1.PositionIndex, 0.3f);
        CreateTween().TweenProperty( skillButton2,"position", skillButton2.PositionIndex, 0.3f);
    }

    public async void AwaitChose(int index)
    {
        switch (readyChange.Count)
        {
            case 1 :
                for (int i = 0; i < FrameContainer.GetChildCount(); i++)
                {
                    var frame = FrameContainer.GetChild<Frame>(i);
                    if(frame.GetIndex() != index)  for (int j = 0; j < frame.SkillButtonContainer.GetChildCount(); j++)
                    {
                        SkillButton button = frame.SkillButtonContainer.GetChild<SkillButton>(j);
                        button.Disabled = true;
                    }
                }
        
                for (int i = 0; i < UntakeContainer.GetChildCount(); i++)
                {
                    var untake = UntakeContainer.GetChild(i);
                    if(untake.GetIndex() != index) for (int j = 0; j < untake.GetChildCount(); j++)
                    {
                        SkillButton button = untake.GetChild<SkillButton>(j);
                        button.Disabled = true;
                    }
                }
                break;
            case 2 :
                ChangePosition(readyChange[0], readyChange[1]);
                await Task.Delay(300);
                readyChange[0].Modulate = new Color(0.9f, 0.9f, 0.9f, 0.9f);
                readyChange[1].Modulate = new Color(0.9f, 0.9f, 0.9f, 0.9f);
                ActivateAllButton();
                readyChange.Clear();
                break;
        }
    }


    

    public void ComfirmTactics()
    {
        for (int i = 0; i < FrameContainer.GetChildCount(); i++)
        {
            List<Skill> newSkills = new List<Skill>();
            var frame = FrameContainer.GetChild<Frame>(i);
            List<SkillButton> sortButtons = frame.SkillButtonContainer.GetChildren().OfType<SkillButton>().OrderBy(x => x.Position.Y).ToList();
            for (int j = 0; j < sortButtons.Count; j++)
            {
                newSkills.Add(sortButtons[j].SelfSkill);
            }
            PlayerInfo.PlayerCharaters[i].Skills = newSkills.ToArray();
        }

        for (int i = 0; i < UntakeContainer.GetChildCount(); i++)
        {
            var untake = UntakeContainer.GetChild(i);
            List<Skill> newSkills = new List<Skill>();
            for (int j = 0; j < untake.GetChildCount(); j++)
            {
                newSkills.Add(untake.GetChild<SkillButton>(j).SelfSkill);
            }
            PlayerInfo.PlayerCharaters[i].UntakeSkills = newSkills.ToList();
        }
        
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
    
    public void ActivateAllButton()
    {
        for (int i = 0; i < FrameContainer.GetChildCount(); i++)
        {
            var frame = FrameContainer.GetChild<Frame>(i);
            for (int j = 0; j < frame.SkillButtonContainer.GetChildCount(); j++)
            {
                SkillButton button = frame.SkillButtonContainer.GetChild<SkillButton>(j);
                button.Disabled = false;
            }
        }
        for (int i = 0; i < UntakeContainer.GetChildCount(); i++)
        {
            var untake = UntakeContainer.GetChild(i);
            for (int j = 0; j < untake.GetChildCount(); j++)
            {
                SkillButton button = untake.GetChild<SkillButton>(j);
                button.Disabled = false;
            }
        }
    }
}
