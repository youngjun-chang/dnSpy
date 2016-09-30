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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class MemberPickerVM : ViewModelBase {
		const int DEFAULT_DELAY_SEARCH_MS = 100;

		public IOpenAssembly OpenAssembly {
			set { openAssembly = value; }
		}
		IOpenAssembly openAssembly;

		public ICommand OpenCommand => new RelayCommand(a => OpenNewAssembly(), a => CanOpenAssembly);

		public bool CanOpenAssembly {
			get { return true; }
			set {
				if (canOpenAssembly != value) {
					canOpenAssembly = value;
					OnPropertyChanged(nameof(CanOpenAssembly));
				}
			}
		}
		bool canOpenAssembly = true;

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged(nameof(SelectedItem));
					if (value != null) {
						searchResult = null;
						OnPropertyChanged(nameof(SearchResult));
					}
					HasErrorUpdated();
				}
			}
		}
		object selectedItem;

		public object SelectedDnlibObject {
			get {
				var res = SearchResult;
				if (res != null) {
					var obj = res.Object;

					if (obj is AssemblyDef && filter.GetResult(obj as AssemblyDef).IsMatch)
						return res.Document;
					if (obj is ModuleDef && filter.GetResult(obj as ModuleDef).IsMatch)
						return res.Document;
					if (obj is IDsDocument && filter.GetResult(obj as IDsDocument).IsMatch)
						return (IDsDocument)obj;
					if (obj is string && filter.GetResult((string)obj, res.Document).IsMatch)
						return (string)obj;
					if (obj is TypeDef && filter.GetResult(obj as TypeDef).IsMatch)
						return obj;
					if (obj is FieldDef && filter.GetResult(obj as FieldDef).IsMatch)
						return obj;
					if (obj is MethodDef && filter.GetResult(obj as MethodDef).IsMatch)
						return obj;
					if (obj is PropertyDef && filter.GetResult(obj as PropertyDef).IsMatch)
						return obj;
					if (obj is EventDef && filter.GetResult(obj as EventDef).IsMatch)
						return obj;
					if (obj is AssemblyRef && filter.GetResult((AssemblyRef)obj).IsMatch)
						return (AssemblyRef)obj;
					if (obj is ModuleRef && filter.GetResult((ModuleRef)obj).IsMatch)
						return (ModuleRef)obj;
				}

				var item = documentTreeView.TreeView.FromImplNode(SelectedItem);
				if (item != null) {
					if (item is IAssemblyDocumentNode && filter.GetResult((item as IAssemblyDocumentNode).Document.AssemblyDef).IsMatch)
						return ((IAssemblyDocumentNode)item).Document;
					else if (item is IModuleDocumentNode && filter.GetResult((item as IModuleDocumentNode).Document.ModuleDef).IsMatch)
						return ((IModuleDocumentNode)item).Document;
					else if (item is IDsDocumentNode && filter.GetResult((item as IDsDocumentNode).Document).IsMatch)
						return ((IDsDocumentNode)item).Document;
					if (item is INamespaceNode && filter.GetResult((item as INamespaceNode).Name, ((item as INamespaceNode).TreeNode.Parent.Data as IModuleDocumentNode).Document).IsMatch)
						return ((INamespaceNode)item).Name;
					if (item is ITypeNode && filter.GetResult((item as ITypeNode).TypeDef).IsMatch)
						return ((ITypeNode)item).TypeDef;
					if (item is IFieldNode && filter.GetResult((item as IFieldNode).FieldDef).IsMatch)
						return ((IFieldNode)item).FieldDef;
					if (item is IMethodNode && filter.GetResult((item as IMethodNode).MethodDef).IsMatch)
						return ((IMethodNode)item).MethodDef;
					if (item is IPropertyNode && filter.GetResult((item as IPropertyNode).PropertyDef).IsMatch)
						return ((IPropertyNode)item).PropertyDef;
					if (item is IEventNode && filter.GetResult((item as IEventNode).EventDef).IsMatch)
						return ((IEventNode)item).EventDef;
					if (item is IAssemblyReferenceNode && filter.GetResult((item as IAssemblyReferenceNode).AssemblyRef).IsMatch)
						return ((IAssemblyReferenceNode)item).AssemblyRef;
					if (item is IModuleReferenceNode && filter.GetResult((item as IModuleReferenceNode).ModuleRef).IsMatch)
						return ((IModuleReferenceNode)item).ModuleRef;
				}

				return null;
			}
		}

		public bool TooManyResults {
			get { return tooManyResults; }
			set {
				if (tooManyResults != value) {
					tooManyResults = value;
					OnPropertyChanged(nameof(TooManyResults));
				}
			}
		}
		bool tooManyResults;

		public ICollectionView SearchResultsCollectionView => searchResultsCollectionView;
		readonly ListCollectionView searchResultsCollectionView;

		public ObservableCollection<ISearchResult> SearchResults { get; }

		public ISearchResult SelectedSearchResult {
			get { return selectedSearchResult; }
			set {
				if (selectedSearchResult != value) {
					selectedSearchResult = value;
					OnPropertyChanged(nameof(SelectedSearchResult));
				}
			}
		}
		ISearchResult selectedSearchResult;

		public string SearchText {
			get { return searchText; }
			set {
				if (searchText != value) {
					bool hasSearchTextChanged = string.IsNullOrEmpty(searchText) != string.IsNullOrEmpty(value);
					searchText = value;
					OnPropertyChanged(nameof(SearchText));
					if (hasSearchTextChanged)
						OnPropertyChanged(nameof(HasSearchText));
					delayedSearch.Start();
				}
			}
		}
		string searchText = string.Empty;
		readonly DelayedAction delayedSearch;

		public bool HasSearchText => !string.IsNullOrEmpty(searchText);

		public ISearchResult SearchResult {
			get { return searchResult; }
			set {
				if (searchResult != value) {
					searchResult = value;
					OnPropertyChanged(nameof(SearchResult));
					if (value != null) {
						selectedItem = null;
						OnPropertyChanged(nameof(SelectedItem));
					}
					HasErrorUpdated();
				}
			}
		}
		ISearchResult searchResult;

		public IEnumerable<IDecompiler> AllLanguages => decompilerService.AllDecompilers;

		public IDecompiler Language {
			get { return decompiler; }
			set {
				if (decompiler != value) {
					decompiler = value;
					OnPropertyChanged(nameof(Language));
					RefreshTreeView();
				}
			}
		}
		IDecompiler decompiler;
		readonly DependencyObject dpiObject;
		readonly IDpiService dpiService;
		readonly IDecompilerService decompilerService;
		readonly IDocumentTreeView documentTreeView;
		readonly IDocumentTreeNodeFilter filter;
		readonly IDocumentSearcherProvider fileSearcherProvider;

		public bool SyntaxHighlight { get; set; }
		public string Title { get; }
		bool CaseSensitive { get; }
		bool MatchWholeWords { get; }
		bool MatchAnySearchTerm { get; }

		public MemberPickerVM(DependencyObject dpiObject, IDpiService dpiService, IDocumentSearcherProvider fileSearcherProvider, IDocumentTreeView documentTreeView, IDecompilerService decompilerService, IDocumentTreeNodeFilter filter, string title, IEnumerable<IDsDocument> assemblies) {
			this.Title = title;
			this.dpiObject = dpiObject;
			this.dpiService = dpiService;
			this.fileSearcherProvider = fileSearcherProvider;
			this.decompilerService = decompilerService;
			this.documentTreeView = documentTreeView;
			this.decompiler = decompilerService.Decompiler;
			this.filter = filter;
			this.delayedSearch = new DelayedAction(DEFAULT_DELAY_SEARCH_MS, DelayStartSearch);
			this.SearchResults = new ObservableCollection<ISearchResult>();
			this.searchResultsCollectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(SearchResults);
			this.searchResultsCollectionView.CustomSort = new SearchResult_Comparer();
			dpiService.DpiChanged += DpiService_DpiChanged;

			foreach (var file in assemblies)
				documentTreeView.DocumentService.ForceAdd(file, false, null);

			documentTreeView.DocumentService.CollectionChanged += (s, e) => Restart();

			this.CaseSensitive = false;
			this.MatchWholeWords = false;
			this.MatchAnySearchTerm = false;
			RefreshTreeView();
		}

		void DpiService_DpiChanged(object sender, WindowDpiChangedEventArgs e) {
			foreach (var vm in SearchResults)
				vm.RefreshUI();
		}

		public bool SelectItem(object item) {
			var node = documentTreeView.FindNode(item);
			if (node == null)
				return false;

			documentTreeView.TreeView.SelectItems(new ITreeNodeData[] { node });
			SelectedItem = documentTreeView.TreeView.ToImplNode(node);

			return true;
		}

		void RefreshTreeView() {
			documentTreeView.SetDecompiler(Language);
			Restart();
		}

		void OpenNewAssembly() {
			if (openAssembly == null)
				throw new InvalidOperationException();

			var file = openAssembly.Open();
			if (file == null)
				return;

			documentTreeView.DocumentService.GetOrAdd(file);
		}

		void DelayStartSearch() => Restart();

		void StartSearch() {
			CancelSearch();
			if (string.IsNullOrEmpty(SearchText))
				SearchResults.Clear();
			else {
				var options = new DocumentSearcherOptions {
					SearchComparer = SearchComparerFactory.Create(SearchText, CaseSensitive, MatchWholeWords, MatchAnySearchTerm),
					Filter = filter,
					SearchDecompiledData = false,
				};
				fileSearcher = fileSearcherProvider.Create(options, documentTreeView);
				fileSearcher.SyntaxHighlight = SyntaxHighlight;
				fileSearcher.Decompiler = Language;
				fileSearcher.ImageOptions = new ImageOptions {
					BackgroundType = BackgroundType.Search,
					DpiObject = dpiObject,
				};
				fileSearcher.ImageOptions.BackgroundType = BackgroundType.Search;
				fileSearcher.OnSearchCompleted += FileSearcher_OnSearchCompleted;
				fileSearcher.OnNewSearchResults += FileSearcher_OnNewSearchResults;
				fileSearcher.Start(documentTreeView.TreeView.Root.DataChildren.OfType<IDsDocumentNode>());
			}
		}
		IDocumentSearcher fileSearcher;
		bool searchCompleted;

		void FileSearcher_OnSearchCompleted(object sender, EventArgs e) {
			if (sender == null || sender != fileSearcher || searchCompleted)
				return;
			searchCompleted = true;
			SearchResults.Remove(fileSearcher.SearchingResult);
			TooManyResults = fileSearcher.TooManyResults;
		}

		void FileSearcher_OnNewSearchResults(object sender, SearchResultEventArgs e) {
			if (sender == null || sender != fileSearcher)
				return;
			Debug.Assert(!searchCompleted);
			if (searchCompleted)
				return;
			foreach (var vm in e.Results)
				SearchResults.Add(vm);
		}

		public void Restart() {
			StopSearch();
			SearchResults.Clear();
			StartSearch();
		}

		void StopSearch() {
			CancelSearch();
			delayedSearch.Cancel();
		}

		public void Clear() {
			SearchText = string.Empty;
			StopSearch();
			SearchResults.Clear();
		}

		void CancelSearch() {
			TooManyResults = false;
			delayedSearch.Cancel();
			if (fileSearcher != null) {
				fileSearcher.Cancel();
				fileSearcher = null;
			}
			searchCompleted = false;
		}

		protected override string Verify(string columnName) {
			if (columnName == nameof(SelectedItem) || columnName == nameof(SearchResult)) {
				if (SelectedItem == null && SearchResult == null)
					return dnSpy_AsmEditor_Resources.PickMember_TypeMustBeSelected;
				if (SelectedDnlibObject == null)
					return GetErrorMessage();
				return string.Empty;
			}
			return string.Empty;
		}

		string GetErrorMessage() => dnSpy_AsmEditor_Resources.PickMember_SelectCorrectNode;

		public override bool HasError {
			get {
				if (!string.IsNullOrEmpty(Verify(nameof(SelectedItem))))
					return true;
				if (!string.IsNullOrEmpty(Verify(nameof(SearchResult))))
					return true;

				return false;
			}
		}

		public void Dispose() => dpiService.DpiChanged -= DpiService_DpiChanged;
	}

	sealed class SearchResult_Comparer : System.Collections.IComparer {
		public int Compare(object x, object y) {
			var a = x as ISearchResult;
			var b = y as ISearchResult;
			if (a == null)
				return 1;
			if (b == null)
				return -1;
			if (a == b)
				return 0;
			return a.CompareTo(b);
		}
	}
}
