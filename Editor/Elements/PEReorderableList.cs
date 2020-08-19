using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    using PositionType = UnityEngine.UIElements.Position;

    internal class ItemDragger : Manipulator
    {
        private PEReorderableList _root;
        private VisualElement _line;
        private Vector2 _startPosition;
        private object _context;

        public ItemDragger(PEReorderableList root, VisualElement item)
        {
            _root = root;
            _line = item;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private void Release()
        {
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.ReleaseMouse();
            _context = null;
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                evt.StopPropagation();
                target.CaptureMouse();
                _startPosition = _root.WorldToLocal(evt.mousePosition);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                _context = _root.StartDragging(_line);
            }
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            Vector2 listRelativeMouse = _root.WorldToLocal(evt.mousePosition);
            _root.EndDragging(_context, _line, listRelativeMouse.y - _startPosition.y, evt.mousePosition);
            evt.StopPropagation();
            Release();
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            evt.StopPropagation();
            _root.ItemDragging(_context, _line, _root.WorldToLocal(evt.mousePosition).y - _startPosition.y, evt.mousePosition);
        }
    }

    internal class ItemSelector : Manipulator
    {
        private PEReorderableList _root;
        private VisualElement _line;

        public ItemSelector(PEReorderableList root, VisualElement item)
        {
            _root = root;
            _line = item;
        }

        protected override void RegisterCallbacksOnTarget() =>
            target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);

        protected override void UnregisterCallbacksFromTarget() =>
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);

        void OnMouseDown(MouseDownEvent e) => _root.Select(_line);
    }

    internal class PEReorderableList : VisualElement
    {
        private int _selectedLine = -1;
        private VisualElement _itemsContainer = null;

        public delegate void ElementMovedDelegate(int movedIndex, int targetIndex);

        public event ElementMovedDelegate onElementMoved;

        private class DraggingContext
        {
            public Rect[] originalPositions;
            public VisualElement[] items;
            public Rect myOriginalPosition;
            public int draggedIndex;
        }

        public void Select(int index)
        {
            if (_selectedLine != -1 && _selectedLine < _itemsContainer.childCount)
                _itemsContainer.ElementAt(_selectedLine).RemoveFromClassList("selected");

            _selectedLine = index;

            if (_selectedLine != -1 && _selectedLine < _itemsContainer.childCount)
                _itemsContainer.ElementAt(_selectedLine).AddToClassList("selected");
        }

        /// <summary>
        /// Select the give item
        /// </summary>
        public void Select(VisualElement item) => Select(_itemsContainer.IndexOf(item));
        
        public object StartDragging(VisualElement item)
        {
            var items = _itemsContainer.Children().ToArray();
            var context = new DraggingContext
            {
                items = items,
                originalPositions = items.Select(t => t.layout).ToArray(),
                draggedIndex = _itemsContainer.IndexOf(item),
                myOriginalPosition = _itemsContainer.layout
            };

            Select(context.draggedIndex);

            for (int i = 0; i < context.items.Length; ++i)
            {
                VisualElement child = context.items[i];
                Rect rect = context.originalPositions[i];
                child.style.position = PositionType.Absolute;
                child.style.left = rect.x;
                child.style.top = rect.y;
                child.style.width = rect.width;
                child.style.height = rect.height;
            }

            item.BringToFront();

            _itemsContainer.style.width = context.myOriginalPosition.width;
            _itemsContainer.style.height = context.myOriginalPosition.height;

            return context;
        }

        public void EndDragging(object ctx, VisualElement item, float offset, Vector2 mouseWorldPosition)
        {
            var context = (DraggingContext)ctx;

            foreach (var child in _itemsContainer.Children())
                child.ResetPositionProperties();

            var hoveredIndex = GetHoveredIndex(context, mouseWorldPosition);
            _itemsContainer.Insert(hoveredIndex != -1 ? hoveredIndex : context.draggedIndex, item);
            _itemsContainer.ResetPositionProperties();

            if (hoveredIndex != -1)
                ElementMoved(context.draggedIndex, hoveredIndex);
        }

        public void ItemDragging(object ctx, VisualElement item, float offset, Vector2 mouseWorldPosition)
        {
            var context = (DraggingContext)ctx;
            var hoveredIndex = GetHoveredIndex(context, mouseWorldPosition);

            item.style.top = context.originalPositions[context.draggedIndex].y + offset;            

            if (hoveredIndex != -1)
            {
                float draggedHeight = context.originalPositions[context.draggedIndex].height;

                if (hoveredIndex < context.draggedIndex)
                {
                    for (int i = 0; i < hoveredIndex; ++i)
                    {
                        context.items[i].style.top = context.originalPositions[i].y;
                    }
                    for (int i = hoveredIndex; i < context.draggedIndex; ++i)
                    {
                        context.items[i].style.top = context.originalPositions[i].y + draggedHeight;
                    }
                    for (int i = context.draggedIndex + 1; i < context.items.Length; ++i)
                    {
                        context.items[i].style.top = context.originalPositions[i].y;
                    }
                }
                else if (hoveredIndex > context.draggedIndex)
                {
                    for (int i = 0; i < context.draggedIndex; ++i)
                    {
                        context.items[i].style.top = context.originalPositions[i].y;
                    }
                    for (int i = hoveredIndex; i > context.draggedIndex; --i)
                    {
                        context.items[i].style.top = context.originalPositions[i].y - draggedHeight;
                    }
                    for (int i = hoveredIndex + 1; i < context.items.Length; ++i)
                    {
                        context.items[i].style.top = context.originalPositions[i].y;
                    }
                }
            }
            else
            {
                for (int i = 0; i < context.items.Length; ++i)
                {
                    if (i != context.draggedIndex)
                        context.items[i].style.top = context.originalPositions[i].y;
                }
            }
        }

        private int GetHoveredIndex (DraggingContext context, Vector2 mouseWorldPosition)
        {
            var mousePosition = _itemsContainer.WorldToLocal(mouseWorldPosition);
            var hoveredIndex = -1;

            for (int i = 0; i < context.items.Length; ++i)
                if (i != context.draggedIndex && context.originalPositions[i].Contains(mousePosition))
                {
                    hoveredIndex = i;
                    break;
                }

            return hoveredIndex;
        }

        protected virtual void ElementMoved(int movedIndex, int targetIndex)
        {
            if (_selectedLine == movedIndex)
                _selectedLine = targetIndex;

            onElementMoved?.Invoke(movedIndex, targetIndex);
        }

        
        public PEReorderableList()
        {
            _itemsContainer = new VisualElement { name = "ListContainer" };

            Add(_itemsContainer);

            this.AddStyleSheetPathWithSkinVariant("ReorderableList");
            AddToClassList("ReorderableList");
        }

        /// <summary>
        /// Add an item to the reorderable list
        /// </summary>
        public void AddItem(VisualElement item)
        {
            _itemsContainer.Add(item);

            item.AddManipulator(new ItemSelector(this, item));
            item.AddManipulator(new ItemDragger(this, item));

            Select(_itemsContainer.childCount - 1);
        }

        public void RemoveAllItems ()
        {
            _itemsContainer.Clear();
            _selectedLine = -1;
        }

        public void RemoveItemAt(int index)
        {
            _itemsContainer.RemoveAt(index);

            if (_selectedLine >= _itemsContainer.childCount)
                Select(_itemsContainer.childCount - 1);
        }

        public VisualElement ItemAt(int index) => _itemsContainer.ElementAt(index);

        public int itemCount => _itemsContainer.childCount;        
    }
}
