# U3
A library for Unity's UIElements, heavily inspired by D3 library for JS

## Purpose
U3 is intended to simplify working with UIElements, especially where the UI needs to be generated from some dynamic data. It is rather simple, so it's suited only for work with limited datasets.

## Concept
All of U3's functions are concentrated in the concept of a Selection. A selection is a set of groups, where each group is a parent element, and a set of 'selected' objects, which are allways descendants of the parent element. 
A selection has three main sets of functions:
* Select and Find - these allow navigating through the hierarchy of UIElements (since selection is immutable, they always return a new selection)
* Bind, Enter, Exit, Join, Merge - this set is used to create and destroy elements to match some input data
* ForEach, Label, OnEvent, Texture - these functions are used to modify the selected elements

## Example
Say you have a list of strings, and you wanted to create a list of buttons where each one corresponds to one string, has this string written on it, and calls a functions with this string as a parameter when clicked.

1. In UIBuilder, create a VisualElement named "button-list", which will server as a container for all the buttons
1. Find this element
```c#
panelRenderer.visualTree.Find("")
```
1. Create a selection that represents a "set of buttons, which are children of 'button-list'"
```c#
panelRenderer.visualTree.Find("")
  .SelectAll<Button>()
```
1. Bind this selection to the list you want. This will do some preprocessing, and create a new selection
```c#
panelRenderer.visualTree.Find("")
  .SelectAll<Button>()
  .Bind(theListOfStrings)
```
1. Join the selection, which will delete all buttons that don't correspond to a string from this list, and create a button for each string that doesn't have one
```c#
panelRenderer.visualTree.Find("")
  .SelectAll<Button>()
  .Bind(theListOfStrings)
  .Join<Button>()
```
1. Set the text of each button to the string in the list. The 'Label' function is invoked for each selected element, and is passed the element itself, and the datum which is bound to it (in this case the string).
```c#
panelRenderer.visualTree.Find("")
  .SelectAll<Button>()
  .Bind(theListOfStrings)
  .Join<Button>()
  .Label( (element,datum) => datum )
```
1. Register an event handler for the button. The passed function is called once the event is fired, with the element, datum, id (order in parent), and the event itself.
```c#
panelRenderer.visualTree.Find("")
  .SelectAll<Button>()
  .Bind(theListOfStrings)
  .Join<Button>()
  .Label( (element,datum) => datum )
  .OnEvent<MouseUpEvent>( (element,datum,id,event) => theCallbackIWanted(datum) )
```
