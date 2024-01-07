import numpy as np
from typing import Any
from openstk.gfx_render import AABB, Frustum

MaximumElementsBeforeSubdivide = 4
MinimumNodeSize = 64.0

class Octree: pass
class Node: pass
class Element: pass

# Octree
class Octree:
    class Element:
        clientObject: Any
        boundingBox: AABB

    class Node:
        parent: Node
        region: AABB
        elements: list[Element]
        children: list[Node]

        def __init__(self, parent: Node, regionMin: np.ndarray, regionSize: np.ndarray):
            self.parent = parent
            self.region = AABB(regionMin, regionMin + regionSize)

        def subdivide(self):
            if not self.children: return # Already subdivided
            subregionSize = self.region.size * 0.5
            center = self.region.min + subregionSize
            self.Children = [
                Node(self, self.region.min, subregionSize),
                Node(self, np.array([center.X, self.region.Min.Y, self.region.Min.Z]), subregionSize),
                Node(self, np.array([self.region.Min.X, center.Y, self.region.Min.Z]), subregionSize),
                Node(self, np.array([center.X, center.Y, self.region.Min.Z]), subregionSize),
                Node(self, np.array([self.region.Min.X, self.region.Min.Y, center.Z]), subregionSize),
                Node(self, np.array([center.X, self.region.Min.Y, center.Z]), subregionSize),
                Node(self, np.array([self.region.Min.X, center.Y, center.Z]), subregionSize),
                Node(self, np.array([center.X, center.Y, center.Z]), subregionSize)
                ]
            remainingElements = []
            for element in self.elements:
                movedDown = False
                for child in Children:
                    if child.region.contains(element.boundingBox):
                        child.insert(element)
                        movedDown = True
                        break
                if not movedDown:
                    remainingElements.append(element)
            self.elements = remainingElements

        @property
        def hasChildren(self) -> bool: return self.children
        @property
        def hasElements(self) -> bool: return self.elements and self.elements.Count > 0

        def insert(self, element: Element):
            if not self.hasChildren and self.hasElements and self.region.size.X > MinimumNodeSize and self.elements.count >= MaximumElementsBeforeSubdivide: self.subdivide()
            inserted = False
            if self.hasChildren:
                elementBB = element.boundingBox
                for child in self.children:
                    if child.region.contains(elementBB):
                        inserted = True
                        child.insert(element)
                        break
            if not inserted:
                if self.elements == null: self.elements = []
                elements.append(element)
        
        def find(self, clientObject: Any, bounds: AABB) -> (Node, int):
            if self.hasElements:
                for i in self.elements.count:
                    if self.elements[i].clientObject == clientObject: return (self, i)
            if self.hasChildren:
                for child in self.children:
                    if child.Region.Contains(bounds): return child.find(clientObject, bounds)
            return (None, -1)

        def clear(self) -> None:
            self.elements = None
            self.children = None

        def query(self, source: AABB | Frustum, results: list[Any]) -> None:
            if isinstance(source, AABB):
                boundingBox = AABB(source)
                if self.hasElements:
                    for element in Elements:
                        if element.boundingBox.intersects(boundingBox): results.append(element.clientObject)
                if self.hasChildren:
                    for child in self.children:
                        if child.region.intersects(boundingBox): child.query(boundingBox, results)
            elif isinstance(source, Frustum):
                frustum = Frustum(source)
                if self.hasElements:
                    for element in self.elements:
                        if frustum.intersects(element.boundingBox): results.append(element.clientObject)
                if self.hasChildren:
                    for child in self.children:
                        if frustum.intersects(child.region): child.query(frustum, results)

    root: Node

    def __init__(self, size: float):
        if size <= 0: raise Exception('size')
        self.root = Node(None, np.array([v := -size * 0.5, v, v]), np.array([v := size, v, v]))

    def insert(self, obj: Any, bounds: AABB) -> None:
        if not obj: raise Exception('obj')
        self.root.insert(Element(clientObject = obj, boundingBox = bounds))

    def remove(self, obj: Any, bounds: AABB) -> None:
        if not obj: raise Exception('obj')
        node, index = self.root.find(obj, bounds)
        if node: node.elements.removeAt(index)

    def update(self, obj: Any, oldBounds: AABB, newBounds: AABB) -> None:
        if not obj: raise Exception('obj')

        node, index = self.root.find(obj, oldBounds)
        if node:
            # Locate the closest ancestor that the new bounds fit inside
            ancestor = node
            while ancestor.parent and not ancestor.region.contains(newBounds): ancestor = ancestor.parent

            # Still fits in same node?
            if ancestor == node:
                # Still check for pushdown
                if node.hasChildren:
                    for child in node.children:
                        if child.region.contains(newBounds):
                            node.elements.removeAt(index)
                            child.insert(Element(clientObject = obj, boundingBox = newBounds))
                            return

                # Not pushed down into any children
                node.elements[index] = Element(clientObject = obj, boundingBox = newBounds)
        else:
            node.elements.removeAt(index)
            ancestor.insert(Element(clientObject = obj, boundingBox = newBounds))

    def clear(self) -> None: self.root.clear()

    def query(self, source: AABB | Frustum) -> list[Any]:
        results = []
        self.root.query(source, results)
        return results
