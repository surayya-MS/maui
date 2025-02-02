﻿#if ANDROID || (IOS && !MACCATALYST)
using System;
using System.Collections.Generic;

namespace Microsoft.Maui.Controls
{
	partial class HideSoftInputOnTappedChangedManager
	{
		IDisposable? _watchingForTaps;
		WeakReference<IView>? _focusedView;

		internal void UpdatePage(ContentPage page)
		{
			if (page.HideSoftInputOnTapped && page.HasNavigatedTo)
			{
				if (!_contentPages.Contains(page))
				{
					_contentPages.Add(page);
					page.NavigatedFrom += OnPageNavigatedFrom;
					SetupHideSoftInputOnTapped();
				}
			}
			else
			{
				RemovePage(page);
			}

			void RemovePage(ContentPage pageToRemove)
			{
				page.NavigatedFrom -= OnPageNavigatedFrom;
				if (_contentPages.Contains(pageToRemove))
					_contentPages.Remove(pageToRemove);

				SetupHideSoftInputOnTapped();
			}

			void OnPageNavigatedFrom(object? sender, NavigatedFromEventArgs e)
			{
				if (sender is ContentPage pageNavigatedFrom)
				{
					RemovePage(pageNavigatedFrom);
				}
			}
		}

		internal IDisposable? UpdateFocusForView(IView _view)
		{
			// Update to new focused view
			if (_view.IsFocused)
			{
				DisconnectFromPlatform();
				_focusedView = new WeakReference<IView>(_view);
			}
			// If currently tracked view became unfocused then disconnect from it
			else if (_view == FocusedView)
			{
				DisconnectFromPlatform();
				_focusedView = null;
			}

			if (!FeatureEnabled)
			{
				DisconnectFromPlatform();
				return null;
			}

			if (_view is not VisualElement ve)
				return null;

			if (!_view.IsFocused)
				return null;

			DisconnectFromPlatform();

			// This view has been set as focused but it's not currently loaded
			var platformView = (_view.Handler as IPlatformViewHandler)?.PlatformView;
			if (platformView is null)
			{
				return null;
			}

			IDisposable? platformToken = SetupHideSoftInputOnTapped(platformView);

#if ANDROID
			var window = ve.Window;
			window.DispatchTouchEvent += OnWindowDispatchedTouch;
#endif
			_watchingForTaps = new ActionDisposable(() =>
			{
				platformToken?.Dispose();
				platformToken = null;
#if ANDROID
				window.DispatchTouchEvent -= OnWindowDispatchedTouch;
				window = null;
#endif
			});

			return _watchingForTaps;
		}

		void DisconnectFromPlatform()
		{
			_watchingForTaps?.Dispose();
			_watchingForTaps = null;
		}

		IView? FocusedView
		{
			get
			{
				if (_focusedView?.TryGetTarget(out IView? view) == true)
				{
					return view;
				}

				return null;
			}
		}
		internal void SetupHideSoftInputOnTapped()
		{
			if (FocusedView is not null)
			{
				UpdateFocusForView(FocusedView);
			}
		}
	}
}
#endif