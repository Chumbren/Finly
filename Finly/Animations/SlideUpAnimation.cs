using Microsoft.Maui.Animations;
using Microsoft.Maui.Controls;

namespace Finly.Animations
{
    public class FadeInAnimation : Behavior<View>
    {
        public int Duration { get; set; } = 500;

        protected override void OnAttachedTo(View view)
        {
            base.OnAttachedTo(view);
            view.Opacity = 0;
            view.TranslationY = 30;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.WhenAll(
                    view.FadeTo(1, (uint)Duration, Easing.CubicOut),
                    view.TranslateTo(0, 0, (uint)Duration, Easing.CubicOut)
                );
            });
        }
    }

    public class SlideUpAnimation : Behavior<View>
    {
        public int Duration { get; set; } = 600;

        protected override void OnAttachedTo(View view)
        {
            base.OnAttachedTo(view);
            view.Opacity = 0;
            view.TranslationY = 50;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.WhenAll(
                    view.FadeTo(1, (uint)Duration, Easing.CubicOut),
                    view.TranslateTo(0, 0, (uint)Duration, Easing.CubicOut)
                );
            });
        }
    }

    public class ScaleAnimation : Behavior<View>
    {
        public int Duration { get; set; } = 400;
        public double StartScale { get; set; } = 0.8;

        protected override void OnAttachedTo(View view)
        {
            base.OnAttachedTo(view);
            view.Scale = StartScale;
            view.Opacity = 0;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.WhenAll(
                    view.FadeTo(1, (uint)Duration, Easing.CubicOut),
                    view.ScaleTo(1, (uint)Duration, Easing.CubicOut)
                );
            });
        }
    }
}