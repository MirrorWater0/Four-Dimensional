# XMLParser

## Meta

- Name: XMLParser
- Source: XMLParser.xml
- Inherits: RefCounted
- Inheritance Chain: XMLParser -> RefCounted -> Object

## Brief Description

Provides a low-level interface for creating parsers for XML files.

## Description

Provides a low-level interface for creating parsers for XML(https://en.wikipedia.org/wiki/XML) files. This class can serve as base to make custom XML parsers. To parse XML, you must open a file with the open() method or a buffer with the open_buffer() method. Then, the read() method must be called to parse the next nodes. Most of the methods take into consideration the currently parsed node. Here is an example of using XMLParser to parse an SVG file (which is based on XML), printing each element and its attributes as a dictionary:

```
var parser = XMLParser.new()
parser.open("path/to/file.svg")
while parser.read() != ERR_FILE_EOF:
    if parser.get_node_type() == XMLParser.NODE_ELEMENT:
        var node_name = parser.get_node_name()
        var attributes_dict = {}
        for idx in range(parser.get_attribute_count()):
            attributes_dict[parser.get_attribute_name(idx)] = parser.get_attribute_value(idx)
        print("The ", node_name, " element has the following attributes: ", attributes_dict)
```

```
var parser = new XmlParser();
parser.Open("path/to/file.svg");
while (parser.Read() != Error.FileEof)
{
    if (parser.GetNodeType() == XmlParser.NodeType.Element)
    {
        var nodeName = parser.GetNodeName();
        var attributesDict = new Godot.Collections.Dictionary();
        for (int idx = 0; idx < parser.GetAttributeCount(); idx++)
        {
            attributesDict[parser.GetAttributeName(idx)] = parser.GetAttributeValue(idx);
        }
        GD.Print($"The {nodeName} element has the following attributes: {attributesDict}");
    }
}
```

## Quick Reference

```
[methods]
get_attribute_count() -> int [const]
get_attribute_name(idx: int) -> String [const]
get_attribute_value(idx: int) -> String [const]
get_current_line() -> int [const]
get_named_attribute_value(name: String) -> String [const]
get_named_attribute_value_safe(name: String) -> String [const]
get_node_data() -> String [const]
get_node_name() -> String [const]
get_node_offset() -> int [const]
get_node_type() -> int (XMLParser.NodeType)
has_attribute(name: String) -> bool [const]
is_empty() -> bool [const]
open(file: String) -> int (Error)
open_buffer(buffer: PackedByteArray) -> int (Error)
read() -> int (Error)
seek(position: int) -> int (Error)
skip_section() -> void
```

## Methods

- get_attribute_count() -> int [const]
  Returns the number of attributes in the currently parsed element. **Note:** If this method is used while the currently parsed node is not NODE_ELEMENT or NODE_ELEMENT_END, this count will not be updated and will still reflect the last element.

- get_attribute_name(idx: int) -> String [const]
  Returns the name of an attribute of the currently parsed element, specified by the idx index.

- get_attribute_value(idx: int) -> String [const]
  Returns the value of an attribute of the currently parsed element, specified by the idx index.

- get_current_line() -> int [const]
  Returns the current line in the parsed file, counting from 0.

- get_named_attribute_value(name: String) -> String [const]
  Returns the value of an attribute of the currently parsed element, specified by its name. This method will raise an error if the element has no such attribute.

- get_named_attribute_value_safe(name: String) -> String [const]
  Returns the value of an attribute of the currently parsed element, specified by its name. This method will return an empty string if the element has no such attribute.

- get_node_data() -> String [const]
  Returns the contents of a text node. This method will raise an error if the current parsed node is of any other type.

- get_node_name() -> String [const]
  Returns the name of a node. This method will raise an error if the currently parsed node is a text node. **Note:** The content of a NODE_CDATA node and the comment string of a NODE_COMMENT node are also considered names.

- get_node_offset() -> int [const]
  Returns the byte offset of the currently parsed node since the beginning of the file or buffer. This is usually equivalent to the number of characters before the read position.

- get_node_type() -> int (XMLParser.NodeType)
  Returns the type of the current node. Compare with NodeType constants.

- has_attribute(name: String) -> bool [const]
  Returns true if the currently parsed element has an attribute with the name.

- is_empty() -> bool [const]
  Returns true if the currently parsed element is empty, e.g. <element />.

- open(file: String) -> int (Error)
  Opens an XML file for parsing. This method returns an error code.

- open_buffer(buffer: PackedByteArray) -> int (Error)
  Opens an XML raw buffer for parsing. This method returns an error code.

- read() -> int (Error)
  Parses the next node in the file. This method returns an error code.

- seek(position: int) -> int (Error)
  Moves the buffer cursor to a certain offset (since the beginning) and reads the next node there. This method returns an error code.

- skip_section() -> void
  Skips the current section. If the currently parsed node contains more inner nodes, they will be ignored and the cursor will go to the closing of the current element.

## Constants

### Enum NodeType

- NODE_NONE = 0
  There's no node (no file or buffer opened).

- NODE_ELEMENT = 1
  An element node type, also known as a tag, e.g. <title>.

- NODE_ELEMENT_END = 2
  An end of element node type, e.g. </title>.

- NODE_TEXT = 3
  A text node type, i.e. text that is not inside an element. This includes whitespace.

- NODE_COMMENT = 4
  A comment node type, e.g. <!--A comment-->.

- NODE_CDATA = 5
  A node type for CDATA (Character Data) sections, e.g. <![CDATA[CDATA section]]>.

- NODE_UNKNOWN = 6
  An unknown node type.
