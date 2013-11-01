﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Dynamo.Models;
using Dynamo.UI;
using Dynamo.Utilities;
using Dynamo.ViewModels;

namespace Dynamo.Nodes
{
    //taken from http://stackoverflow.com/questions/660554/how-to-automatically-select-all-text-on-focus-in-wpf-textbox
    public class ClickSelectTextBox : TextBox
    {
        protected bool selectAllWhenFocused = true;
        protected RoutedEventHandler focusHandler;

        public ClickSelectTextBox()
        {
            focusHandler = new RoutedEventHandler(SelectAllText);
            AddHandler(PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(SelectivelyIgnoreMouseButton), true);
            AddHandler(GotKeyboardFocusEvent, focusHandler, true);
            AddHandler(MouseDoubleClickEvent, focusHandler, true);
        }

        private static void SelectivelyIgnoreMouseButton(
            object sender, MouseButtonEventArgs e)
        {
            // Find the TextBox
            DependencyObject parent = e.OriginalSource as UIElement;
            while (parent != null && !(parent is TextBox))
                parent = VisualTreeHelper.GetParent(parent);

            if (parent != null)
            {
                var textBox = parent as ClickSelectTextBox;
                if (textBox != null && (!textBox.IsKeyboardFocusWithin))
                {
                    // If the text box is not yet focussed, give it the focus and
                    // stop further processing of this click event.
                    textBox.Focus();
                    e.Handled = textBox.selectAllWhenFocused;
                }
            }
        }

        private static void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null)
                textBox.SelectAll();
        }
    }

    public class DynamoTextBox : ClickSelectTextBox
    {
        public event Action OnChangeCommitted;

        private static Brush clear = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 255, 255, 255));
        private static Brush highlighted = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 255, 255, 255));

        #region Class Operational Methods

        public DynamoTextBox()
            : this(string.Empty)
        {
        }

        public DynamoTextBox(string initialText)
        {
            //turn off the border
            Background = clear;
            BorderThickness = new Thickness(1);
            GotFocus += OnGotFocus;
            LostFocus += OnLostFocus;
            LostKeyboardFocus += OnLostFocus;
            Padding = new Thickness(3);
            base.Text = initialText;
            this.Pending = false;
            Style = (Style)SharedDictionaryManager.DynamoModernDictionary["SZoomFadeTextBox"];
            MinHeight = 20;
        }

        public void BindToProperty(System.Windows.Data.Binding binding)
        {
            this.SetBinding(TextBox.TextProperty, binding);
            UpdateDataSource(false);
        }

        #endregion

        #region Class Properties

        private bool pending;

        public bool Pending
        {
            get { return pending; }
            set
            {
                FontStyle = value ? FontStyles.Italic : FontStyles.Normal;
                pending = value;
            }
        }

        /// <summary>
        /// This property hides the base "TextBox.Text" property to remove the 
        /// ability to directly set its value (by-passing the undo recording 
        /// completely). For this reason, there is no setter for this property.
        /// </summary>
        new public string Text
        {
            get { return base.Text; }
        }

        #endregion

        #region Class Event Handlers

        private void OnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            Background = clear;
        }

        private void OnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            Background = highlighted;
            SelectAll();
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            Pending = true;
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                dynSettings.ReturnFocusToSearch();
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            UpdateDataSource(true);
        }

        #endregion

        #region Private Class Helper Methods

        private void UpdateDataSource(bool recordForUndo)
        {
            if (this.Pending)
            {
                var expr = GetBindingExpression(TextProperty);
                if (expr != null)
                {
                    if (false != recordForUndo)
                        PreUpdateModel(expr.DataItem);

                    expr.UpdateSource();
                }

                if (OnChangeCommitted != null)
                    OnChangeCommitted();

                Pending = false;
            }
        }

        private void PreUpdateModel(object dataItem)
        {
            // Attempt get to the data-bound model (if there's any).
            NodeModel nodeModel = dataItem as NodeModel;
            if (null == nodeModel)
            {
                NodeViewModel nodeViewModel = dataItem as NodeViewModel;
                if (null != nodeViewModel)
                    nodeModel = nodeViewModel.NodeModel;
            }

            // If we do get a node/note, record it for undo.
            if (null != nodeModel)
            {
                DynamoModel dynamo = dynSettings.Controller.DynamoModel;
                dynamo.CurrentWorkspace.RecordModelForModification(nodeModel);

                dynSettings.Controller.DynamoViewModel.UndoCommand.RaiseCanExecuteChanged();
                dynSettings.Controller.DynamoViewModel.RedoCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion
    }

    public class StringTextBox : DynamoTextBox
    {
        #region Class Event Handlers

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            // This method is overridden so that the base implementation will 
            // not be called (the base class commits changes once <Enter> key
            // is pressed, not something that a multi-line string edit box needs.
        }

        #endregion
    }

    public class DynamoSlider : Slider
    {
        NodeModel nodeModel;
        public DynamoSlider(NodeModel model)
        {
            nodeModel = model;
        }
        #region Event Handlers
        protected override void OnThumbDragStarted(System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            base.OnThumbDragStarted(e);
            nodeModel.WorkSpace.RecordModelForModification(nodeModel);
        }
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            if (e.OriginalSource is System.Windows.Shapes.Rectangle)
                nodeModel.WorkSpace.RecordModelForModification(nodeModel);
        }
        #endregion
    }

    public class CodeNodeTextBox : DynamoTextBox
    {
        bool shift, enter;
        public CodeNodeTextBox(string s)
            : base(s)
        {
            shift = enter = false;

            //Remove the select all when focused feature
            RemoveHandler(GotKeyboardFocusEvent, focusHandler);

            //Allow for event processing after textbook has been focused to
            //help set the Caret position
            selectAllWhenFocused = false;

            //Set style for Watermark
            this.SetResourceReference(TextBox.StyleProperty, "CodeBlockNodeTextBox");
            this.Tag = "Your code goes here";
        }

        
        #region Event Handlers 
        /// <summary>
        /// To allow users to remove focus by pressing Shift Enter. Uses two bools (shift / enter)
        /// and sets them when pressed/released. Also handle for escape key
        /// </summary>
        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                shift = true;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                enter = true;
            }
            else if (e.Key == Key.Escape)
            {
                HandleEscapeKey();
            }
            if (shift == true && enter == true)
            {
                dynSettings.ReturnFocusToSearch();
                shift = enter = false;
            }
        }
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                shift = false;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                enter = false;
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            CodeBlockNodeModel cbnNode = this.DataContext as CodeBlockNodeModel;
            string originalCode = cbnNode.Code;
            if (this.Text == "")
            {
                //If the node has some value , ie, if it had some user code, then it was not created 
                //just then. Hence record then delete the node
                if (!originalCode.Equals(""))
                {
                    System.Collections.Generic.List<ModelBase> deleteList = new System.Collections.Generic.List<ModelBase>();
                    deleteList.Add(cbnNode);
                    cbnNode.WorkSpace.RecordAndDeleteModels(deleteList);
                }

                //Otherwise, the node was created just then. Hence its creation and deletion 
                //should not be recorded
                else
                {
                    ;//Pop top undo redo part and delete node without recording
                    //or delete node with recording and pop top 2 items
                }
            }
            else
            {
                if (!this.Text.Equals(originalCode))
                    Pending = true;
                base.OnLostFocus(e);
            }
        }

        protected override void  OnTextChanged(TextChangedEventArgs e)
        {
            e.Handled = true; // To block out the Parents event handler from being called
        }
        #endregion

<<<<<<< HEAD
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            ;//hide base
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            if (!this.Text.Equals((DataContext as CodeBlockNodeModel).Code))
                Pending = true;
            base.OnLostFocus(e);
=======

        #region Private Methods
        private void HandleEscapeKey()
        {
            string originalCode = (this.DataContext as CodeBlockNodeModel).Code;
            if (this.Text.Equals(originalCode))
            {
                dynSettings.ReturnFocusToSearch();
            }
            else
            {
                (this as TextBox).Text = originalCode;
            }
>>>>>>> WaterMark and Escape(without undo fix):
        }
        #endregion
    }
}
