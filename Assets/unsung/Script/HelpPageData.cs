using UnityEngine;

[CreateAssetMenu(menuName = "Help/Help Page Data")]
public class HelpPageData : ScriptableObject
{
    public HelpPageType helpPageType;

    [TextArea(1, 5)]
    public string title;

    [TextArea(5, 20)]
    public string message;

    public Sprite image;
}
