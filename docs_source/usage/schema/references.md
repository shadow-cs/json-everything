# Managing references (`$ref`)

JsonSchema<nsp>.Net handles all references as defined in the draft 2019-09 version of the JSON Schema specification.  This is *not* a change as of v11.0.0;  JsonSchema<nsp>.Net has always behaved this way.  The only change for draft 2019-09 schemas in JsonSchema<nsp>.Net is that `$ref` can now exist alongside other keywords; for earlier drafts, keywords as siblings to `$ref` will be ignored.

## Automatic resolution

JsonSchema<nsp>.Net will not automatically download schemas from URIs that look like network locations.  This may be added in future versions as an option, but it is not supported at this time.

## Schema registration

In order to resolve references more quickly, JsonSchema<nsp>.Net maintains two schema registries for all schemas and subschemas that it has encountered.  The first is a global registry, and the second is a local registry that is passed around on the validation context.  If a schema is not found in the local registry, it will automatically fall back to the global registry.

A `JsonSchema` instance will automatically register itself upon calling `Validate()`.  However, there are some cases where this may be insufficient.  For example, in cases where schemas are separated across multiple files, it is necessary to register the schema instances prior to validation.

For example, given these two schemas

```json
{
  "$id": "http://localhost/my-schema",
  "$type": "object",
  "properties": {
    "refProp": { "$ref": "http://localhost/random-string" }
  }
}

{
  "$id": "http://localhost/random-string",
  "type": "string"
}
```

You must register `random-string` before you attempt to validate with `my-schema`.

```c#
var randomString = JsonSchema.FromFile("random-string.json");
SchemaRegistry.Global.Register("http://localhost/random-string", randomString);
```

Now JsonSchema<nsp>.Net will be able to resolve the reference.