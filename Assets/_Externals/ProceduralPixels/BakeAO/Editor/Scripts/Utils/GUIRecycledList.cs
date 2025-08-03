/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using UnityEngine;
using System;

namespace ProceduralPixels.BakeAO.Editor
{
    internal class GUIRecycledList
    {
        public float singleElementHeight;
        public ElementsCountMethod numberOfElements;
        public DrawElementMethod drawElement;
        public DrawEmptyContainerMethod drawEmptyContainerMethod;

        public GUIRecycledList(float singleElementHeight, DrawElementMethod drawElement, ElementsCountMethod numberOfElements, DrawEmptyContainerMethod drawEmptyContainerMethod = null)
        {
            this.numberOfElements = numberOfElements;
            this.singleElementHeight = singleElementHeight;
            this.drawElement = drawElement;
            this.drawEmptyContainerMethod = drawEmptyContainerMethod;
        }

        public delegate int ElementsCountMethod();
        public delegate void DrawElementMethod(int index, Rect rect);
        public delegate void DrawEmptyContainerMethod(Rect contentRect);

        private float sliderState = 0;
        const float ScrollBarWidth = 13;

        public void Draw(Rect listAreaRect)
        {
            // Scrolling using mouse scroll wheel
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.ScrollWheel)
                if (listAreaRect.Contains(currentEvent.mousePosition))
                    sliderState += currentEvent.delta.y;

            // Calculate rendering areas and number of elements
            float elementsPerView = listAreaRect.height / singleElementHeight;

            var sliderRect = listAreaRect;
            sliderRect.xMin += sliderRect.width - ScrollBarWidth;
            sliderRect.width = ScrollBarWidth;

            var contentRect = listAreaRect;
            contentRect.width -= ScrollBarWidth;

            // Render slider
            sliderState = GUI.VerticalScrollbar(sliderRect, sliderState, Mathf.Min(elementsPerView, numberOfElements()), 0, numberOfElements());

            if (numberOfElements() == 0)
            {
                if (drawEmptyContainerMethod == null)
                    GUI.Box(contentRect, "");
                else 
                    drawEmptyContainerMethod(contentRect);
                return;
            }
            else
                GUI.Box(contentRect, "");

            // Render list elements
            int firstElementIndex = (int)(sliderState);
            int lastElementIndex = (int)(firstElementIndex + elementsPerView - 1);
            lastElementIndex = Math.Min(lastElementIndex, numberOfElements() - 1);
            int drawnElementIndex = 0;
            for (int elementIndex = firstElementIndex; elementIndex <= lastElementIndex; elementIndex++)
            {
                Rect elementRect = contentRect;
                elementRect.yMin += drawnElementIndex * singleElementHeight;
                elementRect.height = singleElementHeight;

                if (elementIndex >= numberOfElements())
                    throw new IndexOutOfRangeException("Trying to draw element with invalid index");

                drawElement?.Invoke(elementIndex, elementRect);
                lastElementIndex = Math.Min(lastElementIndex, numberOfElements() - 1);

                drawnElementIndex++;
            }
        }
    }
}