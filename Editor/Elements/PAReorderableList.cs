using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    using PositionType = UnityEngine.UIElements.Position;

    internal enum ReorderableListDirection
    {
        Horizontal,
        Vertical
    }

    internal class PAItemDragger : Manipulator
    {
        private PAReorderableList _root;
        private VisualElement _line;
        private Vector2 _startPosition;
        private object _context;

        public PAItemDragger(PAReorderableList root, VisualElement item)
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
            if (evt.button != 0)
                return;

            evt.StopPropagation();
            target.CaptureMouse();
            _startPosition = _root.WorldToLocal(evt.mousePosition);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            if(_context != null)
               _root.EndDragging(_context, _line, evt.mousePosition);

            evt.StopPropagation();
            Release();
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            evt.StopPropagation();

            var position = _root.WorldToLocal(evt.mousePosition);
            if (_context == null && (position - _startPosition).magnitude > 2)
            {
                _startPosition = position;
                _context = _root.StartDragging(_line);
            }
            else if (_context != null)
                _root.ItemDragging(_context, _line, _root.WorldToLocal(evt.mousePosition) - _startPosition, evt.mousePosition);
        }
    }

    internal class PAItemSelector : Manipulator
    {
        private PAReorderableList _root;
        private VisualElement _line;

        public PAItemSelector(PAReorderableList root, VisualElement item)
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

    internal class PAReorderableList : VisualElement
    {
        private int _selectedLine = -1;
        private VisualElement _itemsContainer = null;

        public delegate void ItemSelectedDelegate(int selectedIndex);
        public delegate void ItemMovedDelegate(int movedIndex, int targetIndex);

        public event ItemMovedDelegate onItemMoved;
        public event ItemSelectedDelegate onItemSelected;

        private class DraggingContext
        {
            public Rect[] originalPositions;
            public VisualElement[] items;
            public Rect myOriginalPosition;
            public int draggedIndex;
        }

        public ReorderableListDirection direction { get; set; } = ReorderableListDirection.Vertical;

        public void Select(int index)
        {
            index = Mathf.Clamp(index, -1, _itemsContainer.childCount - 1);

            if (_selectedLine == index)
                return;

            if (_selectedLine != -1 && _selectedLine < _itemsContainer.childCount)
                _itemsContainer.ElementAt(_selectedLine).RemoveFromClassList("selected");

            _selectedLine = index;

            if (_selectedLine != -1 && _selectedLine < _itemsContainer.childCount)
                _itemsContainer.ElementAt(_selectedLine).AddToClassList("selected");

            if(_selectedLine >= 0)
                onItemSelected?.Invoke(_selectedLine);
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

        public void EndDragging(object ctx, VisualElement item, Vector2 mouseWorldPosition)
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

        public void ItemDragging(object ctx, VisualElement item, Vector2 offset, Vector2 mouseWorldPosition)
        {
            var context = (DraggingContext)ctx;
            var hoveredIndex = GetHoveredIndex(context, mouseWorldPosition);

            if (direction == ReorderableListDirection.Vertical)
            {
                item.style.top = context.originalPositions[context.draggedIndex].y + offset.y;

                if (hoveredIndex != -1)
                {
                    var draggedHeight = context.originalPositions[context.draggedIndex].height;

                    if (hoveredIndex < context.draggedIndex)
                    {
                        for (int i = 0; i < hoveredIndex; ++i)
                            context.items[i].style.top = context.originalPositions[i].y;
                        for (int i = hoveredIndex; i < context.draggedIndex; ++i)
                            context.items[i].style.top = context.originalPositions[i].y + draggedHeight;
                        for (int i = context.draggedIndex + 1; i < context.items.Length; ++i)
                            context.items[i].style.top = context.originalPositions[i].y;
                    }
                    else if (hoveredIndex > context.draggedIndex)
                    {
                        for (int i = 0; i < context.draggedIndex; ++i)
                            context.items[i].style.top = context.originalPositions[i].y;
                        for (int i = hoveredIndex; i > context.draggedIndex; --i)
                            context.items[i].style.top = context.originalPositions[i].y - draggedHeight;
                        for (int i = hoveredIndex + 1; i < context.items.Length; ++i)
                            context.items[i].style.top = context.originalPositions[i].y;
                    }
                }
                else
                {
                    for (int i = 0; i < context.items.Length; ++i)
                        if (i != context.draggedIndex)
                            context.items[i].style.top = context.originalPositions[i].y;
                }
            }
            else
            {
                item.style.left = context.originalPositions[context.draggedIndex].x + offset.x;

                if (hoveredIndex != -1)
                {
                    var draggedWidth = context.originalPositions[context.draggedIndex].width;

                    if (hoveredIndex < context.draggedIndex)
                    {
                        for (int i = 0; i < hoveredIndex; ++i)
                            context.items[i].style.left = context.originalPositions[i].x;
                        for (int i = hoveredIndex; i < context.draggedIndex; ++i)
                            context.items[i].style.left = context.originalPositions[i].x + draggedWidth;
                        for (int i = context.draggedIndex + 1; i < context.items.Length; ++i)
                            context.items[i].style.left = context.originalPositions[i].x;
                    }
                    else if (hoveredIndex > context.draggedIndex)
                    {
                        for (int i = 0; i < context.draggedIndex; ++i)
                            context.items[i].style.left = context.originalPositions[i].x;
                        for (int i = hoveredIndex; i > context.draggedIndex; --i)
                            context.items[i].style.left = context.originalPositions[i].x - draggedWidth;
                        for (int i = hoveredIndex + 1; i < context.items.Length; ++i)
                            context.items[i].style.left = context.originalPositions[i].x;
                    }
                }
                else
                {
                    for (int i = 0; i < context.items.Length; ++i)
                        if (i != context.draggedIndex)
                            context.items[i].style.left = context.originalPositions[i].x;
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

            onItemMoved?.Invoke(movedIndex, targetIndex);
        }

        
        public PAReorderableList()
        {
            _itemsContainer = new VisualElement { name = "ItemsContainer" };

            Add(_itemsContainer);

            this.AddStyleSheetPath("PEReorderableList");
            AddToClassList("ReorderableList");
        }

        /// <summary>
        /// Add an item to the reorderable list
        /// </summary>
        public void AddItem(VisualElement item)
        {
            _itemsContainer.Add(item);

            item.AddManipulator(new PAItemSelector(this, item));
            item.AddManipulator(new PAItemDragger(this, item));

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
