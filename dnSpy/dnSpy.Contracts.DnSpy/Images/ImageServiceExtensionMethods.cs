﻿/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Windows;
using System.Windows.Controls;

namespace dnSpy.Contracts.Images {
	static class ImageServiceExtensionMethods {
		public static void Add16x16Image(this IImageService self, DependencyObject dpiObject, MenuItem menuItem, ImageReference imageReference, bool isCtxMenu, bool? enable = null) {
			var options = new ImageOptions { DpiObject = dpiObject };
			self.Add16x16Image(options, menuItem, imageReference, isCtxMenu, enable);
		}

		public static void Add16x16Image(this IImageService self, ImageOptions imageOptions, MenuItem menuItem, ImageReference imageReference, bool isCtxMenu, bool? enable = null) {
			var options = imageOptions.Clone();
			options.BackgroundType = isCtxMenu ? BackgroundType.ContextMenuItem : BackgroundType.AppMenuMenuItem;
			var image = new Image {
				Width = 16,
				Height = 16,
				Source = self.GetImage(imageReference, options),
			};
			menuItem.Icon = image;
			if (enable == false)
				image.Opacity = 0.3;
		}
	}
}
