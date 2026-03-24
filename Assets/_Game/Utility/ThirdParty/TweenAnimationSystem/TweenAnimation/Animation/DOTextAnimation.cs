using DG.Tweening;
using TMPro;

namespace Eco.TweenAnimation
{
    public class DOTextAnimation : IAnimation
    {
        private AnimationFactory _factory;
        private TextMeshProUGUI _textComponent;
        private BaseOptions _options;
        private TextOptions _customOptions;
        
        public void Initialized(AnimationFactory animationFactory)
        {
            _factory = animationFactory;
            _textComponent = animationFactory.TweenAnimation.TextMeshProComponent;
            _options = _factory.TweenAnimation.BaseOptions;
            _customOptions = _factory.TweenAnimation.TextOptions;
            
            // Set default text if ToText is empty
            if(string.IsNullOrEmpty(_customOptions.ToStr) && _textComponent != null)
                _customOptions.ToStr = _textComponent.text;
        }

        public void SetAnimationFrom()
        {
            if (_textComponent != null)
                _textComponent.text = _customOptions.FromStr;
        }

        public Tweener Show()
        {
            SetAnimationFrom();
            return _textComponent
                .DOText(_customOptions.ToStr, _options.Duration, _customOptions.RichTextEnabled, _customOptions.ScrambleMode, _customOptions.ScrambleChars)
                .SetEase(_options.ShowEase)
                .SetUpdate(_options.IgnoreTimeScale)
                .SetDelay(_options.StartDelay);
        }

        public Tweener Hide()
        {
            if (_textComponent != null)
                _textComponent.text = _customOptions.ToStr;
            return _textComponent
                .DOText(_customOptions.FromStr, _options.Duration, _customOptions.RichTextEnabled, _customOptions.ScrambleMode, _customOptions.ScrambleChars)
                .SetEase(_options.HideEase)
                .SetUpdate(_options.IgnoreTimeScale)
                .SetDelay(_options.StartDelay);
        }
    }
}