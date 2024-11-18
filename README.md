# RagSharp Library

Turns your application or existing methods into RAG-ready functions, enabling LLMs to help execute your code and make your application smarter.

# RagSharpPropertyAttribute and RagSharpToolAttribute

The `RagSharpPropertyAttribute` and `RagSharpToolAttribute` are custom attributes in the RagSharp library, designed to enrich function schemas for retrieval-augmented generation (RAG) purposes. These attributes help define metadata for the properties and methods used within the library, thereby allowing for more informative and controlled behavior when using the OpenAI API to turn C# methods into RAG-ready functions.

## RagSharpPropertyAttribute

### Namespace

```csharp
using RagSharpLib.Attributes;
```

### Declaration

```csharp
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class RagSharpPropertyAttribute : Attribute
```

### Parameters

- **`string description`**: A textual description of the property. This description is used to provide context about the property in the function schema generated by RagSharp.
- **`bool required`** *(optional)*: Specifies whether the annotated property is mandatory when calling the function. Defaults to `false` if not explicitly provided.

### Constructor

```csharp
public RagSharpPropertyAttribute(string description, bool required = false)
```

- **`description`**: A brief description explaining the purpose or role of the property.
- **`required`**: A boolean flag indicating if this property is required (`true` if mandatory; otherwise, `false`).

### Usage Example

```csharp
    public class User
    {
        [RagSharpProperty("The Email of the user", true)]
        public string Email { get; set; }

        [RagSharpProperty("The firstname of the user")]
        public string FirstName { get; set; }

        [RagSharpProperty("The LastName of the user")]
        public string LastName { get; set; }

        [RagSharpProperty("The user comments")]
        public List<UserComment> Comments { get; set; }
    }
```

### Attribute Target

The `RagSharpPropertyAttribute` can be applied to:

- **Properties**: Annotate properties to add a description and specify if they are required when using them in the function schema.
- **Parameters**: Annotate method parameters to enforce requirements and provide detailed descriptions for the generated RAG schema.

### Example Use Case

Imagine you are creating a method that performs a personalized search based on user preferences. You could annotate each parameter or property to describe its role and specify if it's required.

```csharp
public class SearchRequest
{
    [RagSharpProperty("The keyword to be searched", true)]
    public string Keyword { get; set; }

    [RagSharpProperty("The category to filter the search results")]
    public string Category { get; set; }
}
```

In this example, `Keyword` is a required parameter, ensuring that it is always provided when invoking the search function, while `Category` is optional.

### Summary

The `RagSharpPropertyAttribute` serves as a vital utility for defining property and parameter metadata within the RagSharp library. By adding descriptive information and indicating required fields, this attribute contributes to generating well-defined and structured function schemas for RAG operations, improving both clarity and usability for developers and the underlying model.

## RagSharpToolAttribute

The `RagSharpToolAttribute` is a custom attribute used to annotate methods within the RagSharp library. This attribute ensures that methods are included in the function schema for LLM calls, allowing for precise and enriched interaction.

### Namespace

```csharp
using RagSharpLib.Attributes;
```

### Declaration

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class RagSharpToolAttribute : Attribute
```

### Parameters

- **`string description`**: A textual description of what the function does. This helps in providing context to the LLM for when and how to use the method.
- **`bool additionalProperties`** *(optional)*: Indicates whether additional properties can be added when invoking the function. Defaults to `false`. When set to `true`, the LLM can include extra properties, but `strict` must be set to `false`.
- **`bool strict`** *(optional)*: Specifies whether the LLM should follow the exact schema when calling the function. Defaults to `true`.

### Constructor

```csharp
public RagSharpToolAttribute(string description, bool additionalProperties = false, bool strict = true)
```

- **`description`**: A brief description explaining the purpose or behavior of the method.
- **`additionalProperties`**: A boolean flag that, when `true`, allows the LLM to add more properties as needed (requires `strict` to be `false`).
- **`strict`**: A boolean flag indicating if the LLM should adhere strictly to the defined schema (`true` if strict adherence is required; otherwise, `false`).

### Usage Example

```csharp
 [RagSharpTool("This method retrieve a user by id")]
 public async Task<User> GetUserById([RagSharpProperty("The userId of the user", true)] int Id)
 {
     //Implementation 
     return result;
 }

 public class CreateComment
  {
      [RagSharpProperty("The email of the user", true)]
      public string Email { get; set; }

      [RagSharpProperty("The Id of the user on user table", true)]
      public int UserId { get; set; }

      [RagSharpProperty("The comment of the users", true)]
      public int UserId { get; set; }
  }
[RagSharpTool("This method create user comment")]
public async Task<CreateComment> CreateComment(CreateComment comment)
 {
    //Imeplenetation
     return result;
 }

 [RagSharpTool("This method retrieve user comment by id and email")]
 public async Task<User> GetUserCommentByIdAndEmail([RagSharpProperty("The userId of the user", true)] 
 int Id, [RagSharpProperty("The email of the user",true)] string email)
 {
     //Implementation 
     return result;
 }
```

### Attribute Target

The `RagSharpToolAttribute` can be applied to:

- **Methods**: Annotate methods to provide a description, and control the LLM's behavior regarding strictness and the inclusion of additional properties.

### Summary

The `RagSharpToolAttribute` is a key utility for annotating methods in the RagSharp library, providing descriptions and controlling how the LLM interacts with those methods. By specifying properties like `strict` and `additionalProperties`, this attribute ensures that developers can create both rigid and flexible interaction schemas, depending on the use case.



## RagSharp Instance Initialization and Usage

The RagSharp library also provides methods to initialize an instance and interact with the LLM, allowing you to build and execute RAG-based workflows.

### Methods for Initializing RagSharp

- **`AddTool(Type T)`**: Adds a tool (class) to the RagSharp instance. The tool can be a static, instance, or generic class, and multiple tools can be added by calling `AddTool` multiple times.
- **`Build()`**: Verifies if at least one tool has been added to the instance.
- **`CreateAsync<T>()`**  or **`CreateAsync()`**: Creates a new interaction with the LLM. The generic version (`CreateAsync<T>`) deserializes the response from the LLM to the specified type `T`, while the non-generic version returns the raw LLM response as a text string.

### Usage Example

```csharp
var openai = await new RagSharpOpenAIChat(new Settings
{
    APIKey = "sk-proj-D****",
    Model = "gpt-4o-mini",
})
    .AddTool(typeof(Onboarding))
    .AddTool(typeof(Util))
    .Build()
    .CreateAsync<Users>(new RagSharpOpenAIChatMessages
    {
        Strict = true,// This tell RagSharp to make the final output strict
        AdditionalProperties = false, // This tell RagSharp to add more property to the final out, if set to true strict must be false
        LogModelEvent = true, // tell Ragsharp to log event whenever the model is called
        Messages = new()
        {
            new Messages
            {
               Content = "You are a helpful customer support assistant. " +
               "Utilize the available tools to assist the user. Ensure every response involves a tool " +
               "call until reaching the final outcome. if the final result is a question call another tool",
               Role = "system",
            },
            new Messages
            {
                Role = "user",
                Content = "show me all the comments from John and Alice"
            }
        }
    });
```
```csharp
   {
  "UserList": [
    {
      "Email": "john.doe@example.com",
      "LoginMode": 0,
      "FirstName": "John",
      "LastName": "Doe",
      "Id": 1,
      "Comments": [
        {
          "Id": 1,
          "UserId": 1,
          "CreatedAt": "2024-11-01T10:30:00",
          "Comment": "Really enjoying the new features! Keep it up."
        },
        {
          "Id": 2,
          "UserId": 1,
          "CreatedAt": "2024-11-03T15:45:00",
          "Comment": "Had some trouble logging in, but it works fine now."
        },
        {
          "Id": 3,
          "UserId": 1,
          "CreatedAt": "2024-11-05T18:00:00",
          "Comment": "Looking forward to more updates!"
        }
      ]
    },
    {
      "Email": "alice.johnson@example.com",
      "LoginMode": 2,
      "FirstName": "Alice",
      "LastName": "Johnson",
      "Id": 3,
      "Comments": [
        {
          "Id": 7,
          "UserId": 3,
          "CreatedAt": "2024-11-01T11:10:00",
          "Comment": "Excellent support from the team."
        },
        {
          "Id": 8,
          "UserId": 3,
          "CreatedAt": "2024-11-03T16:30:00",
          "Comment": "Microsoft login works perfectly."
        },
        {
          "Id": 9,
          "UserId": 3,
          "CreatedAt": "2024-11-05T19:00:00",
          "Comment": "I love the clean design of the interface."
        }
      ]
    }
  ]
}
```


### Subscribing to LLM Events

RagSharp allows you to subscribe to LLM events to monitor or handle specific actions during interactions.

- **`EventAggregator.Subscribe<LLMEvent>(OnCustomEvent)`**: Subscribes to the LLM event to handle custom logic.

### Usage Example for Subscribing to Events

```csharp
EventAggregator.Subscribe<LLMEvent>(OnCustomEvent);

void OnCustomEvent(LLMEvent customEvent)
{
    Console.WriteLine($"Subscriber received: {customEvent.Data} {customEvent.EventType}");
}
```

In this example, the `OnCustomEvent` method will handle the `LLMEvent` whenever it is triggered. This allows you to react to events in real time and log or modify behavior accordingly.

---

[LinkedIn Profile](https://www.linkedin.com/in/oscar-itaba-4b5107a0/)

© 2024
