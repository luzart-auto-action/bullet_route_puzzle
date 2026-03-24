using UnityEngine;
using Eco.TweenAnimation;

public class TestDOText : MonoBehaviour
{
    public EcoTweenAnimation textAnimation;
    
    void Start()
    {
        if (textAnimation != null && textAnimation.Animation == EAnimation.DOText)
        {
            Debug.Log("DOText Animation is ready!");
        }
    }
    
    [ContextMenu("Test Show")]
    public void TestShow()
    {
        if (textAnimation != null)
            textAnimation.Show();
    }
    
    [ContextMenu("Test Hide")]
    public void TestHide()
    {
        if (textAnimation != null)
            textAnimation.Hide();
    }
}