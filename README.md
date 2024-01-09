# UJSON
Lightweight JSON parsing and utilities <br/>

# Usage
## UJSON Parsing
Parse JSON in String format into JObject class using JObject's Parse function. 
```
string text = @"
{
  ""name"": ""Usejun"",
  ""age"": 888,
  ""height"": 11111111,
  ""skill"": {
    ""Psychokinesis"": false,
    ""Prognosis"": false
  },
  ""test"": [
    1,
    2,
    3
  ]
}";
  
JObject json = JObject.Parse(text);
```


## UJSON Utility
### 1. indexing
A JObject is a name or, if it is an array, a number that can be accessed by index.

The value that a JObject gives as an index is also a JObject, so it can be accessed as an index again.

```
string name = json["name"]; // -> Usejun
int n = json["test"][0]; // -> 1
```

### 2. Convert JSON text
ToText, a function of JObject, allows you to turn a JObject class back into JSON as a String.

```
Console.WriteLine(json.ToText());
/*
  {
    "name": "Usejun",
    "age": 888,
    "height": 11111111,
    "skill": {
      "Psychokinesis": false,
      "Prognosis": false
    },
    "test": [
      1,
      2,
      3,
    ]
  }
*/
```

### 3. Strong type conversion
JSON data is divided into six classes: JObject, JArray, JString, JNumber, JBoolean, and JNull, of which JArray is a List<JObject>, JString is a string, JNumber is a double, and JBoolean is a bool, and can be explicitly converted to each type.

```
string name = json["name"]; // -> Usejun
int age = json["name"]; // -> 888
List<int> test = json["test"]; // -> { 1, 2, 3 }
```

### 4. Simple data Editing
JObjects have Add, Remove, and Update functions. These functions allow you to make edits to JSON data.

```
json.Add("lv", 123);
json.AddArray("what", 1, 2, 3, 4, 5, 6);
json["what"].Add(7);
json.AddObject("goods", ("book", "read"), ("computer", true));

Console.WriteLine(json.ToText());
/*
  {
    "name": "Usejun",
    "age": 888,
    "height": 11111111,
    "skill": {
      "Psychokinesis": false,
      "Prognosis": false
    },
    "test": [
      1,
      2,
      3
    ],
    "lv": 123,
    "what": [
      1,
      2,
      3,
      4,
      5,
      6,
      7
    ],
    "goods": {
      "book": "read",
      "computer": true
    }
  }
*/

```

### 5. modification restrictions
You can limit access to modify a JObject through the JAccess Enum.

※ By default, all modifications are allowed.

```
string text = ...;

JObject json = JObject.Parse(text, JAccess.Immutable);

json["name"] = "UUseJJJun"; // -> throw Exception   

```

## UJSON Create
UJSON can generate JSON directly. All Add functions are expected to return themselves using method chaining.

```
JObject json = new JObject();

json.Add("name", "Arthur Fleck")
    .Add("age", "?")
    .Add("sex", "male")
    .AddObject("family", ("mother", "Penny Fleck"))
    .AddArray("career", "Clown")
    .Add("marital status", false)
    .Add("cool", true);

Console.WriteLine(json.ToText());
/*
{
  "name": "Arthur Fleck",
  "age": "?",
  "sex": "male",
  "family": {
    "mother": "Penny Fleck"
  },
  "career": [
    "Clown"
  ],
  "marital status": false,
  "cool": true
}
*/
```
