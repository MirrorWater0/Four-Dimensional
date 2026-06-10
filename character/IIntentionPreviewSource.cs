using System.Threading.Tasks;
using Godot;

public interface IIntentionPreviewSource
{
    Character SourceCharacter { get; }
    Control IntentionControl { get; }
    Skill CurrentIntentionSkill { get; }

    bool HasActiveStun();
    Task DisappearIntention();
    void DisplayIntention();
    void RefreshIntentionDisplayForCurrentState();
    void RefreshNextIntentionAfterAction();
}
