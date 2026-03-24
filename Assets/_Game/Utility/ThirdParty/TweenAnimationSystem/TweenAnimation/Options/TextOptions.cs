using Sirenix.OdinInspector;
using DG.Tweening;

namespace Eco.TweenAnimation
{
    [System.Serializable]
    public class TextOptions
    {
        [FoldoutGroup("Text Options")] public string FromStr = "";
        [FoldoutGroup("Text Options")] public string ToStr = "";
        [FoldoutGroup("Text Options")] public bool RichTextEnabled = true;
        [FoldoutGroup("Text Options")] public ScrambleMode ScrambleMode = ScrambleMode.None;
        [FoldoutGroup("Text Options")] public string ScrambleChars = "";
    }
}