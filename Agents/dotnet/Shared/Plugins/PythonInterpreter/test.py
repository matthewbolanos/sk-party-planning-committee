from typing import Any, Dict, List, Union
from pydantic import BaseModel, Field
import re

def normalize_namespace(namespace: str) -> str:
    # Convert "LightPlugin" or "lightPlugin" to "light_plugin"
    namespace = re.sub('([a-z])([A-Z])', r'\1_\2', namespace).lower()
    return namespace

def to_camel_case(snake_str: str) -> str:
    components = snake_str.split('_')
    return components[0].capitalize() + ''.join(x.capitalize() for x in components[1:])

def parse_openapi_spec(openapi_spec: Dict[str, Any], namespace: str) -> str:
    namespace = normalize_namespace(namespace)
    components = openapi_spec.get("components", {})
    schemas = components.get("schemas", {})
    pydantic_models = []

    for schema_name, schema in schemas.items():
        model_code = json_schema_to_pydantic_model(schema, model_name=schema_name, namespace=namespace)
        pydantic_models.append(model_code)

    return "\n\n".join(pydantic_models)

def json_schema_to_pydantic_model(schema: Dict[str, Any], model_name: str = None, namespace: str = "") -> str:
    model_name = model_name or schema.get("title", "GeneratedModel")
    namespace_prefix = to_camel_case(namespace) + '__'
    full_model_name = namespace_prefix + model_name
    properties = schema.get("properties", {})
    required = schema.get("required", [])

    model_lines = [f"class {full_model_name}(BaseModel):"]
    for prop_name, prop_schema in properties.items():
        prop_type = get_pydantic_type(prop_schema, prop_name)
        format_comment = f"  # format: {prop_schema['format']}" if "format" in prop_schema else ""
        default = " = None" if prop_name not in required else ""
        model_lines.append(f"    {prop_name}: {prop_type}{default}{format_comment}")
    
    return "\n".join(model_lines)

def get_pydantic_type(schema: Dict[str, Any], prop_name: str) -> str:
    type_mapping = {
        "string": "str",
        "number": "float",
        "integer": "int",
        "boolean": "bool"
    }

    json_type = schema.get("type")
    if json_type == "array":
        items_schema = schema.get("items", {})
        items_type = get_pydantic_type(items_schema, prop_name)
        return f"List[{items_type}]"
    
    if json_type == "object":
        nested_model_name = prop_name.capitalize()
        nested_model = json_schema_to_pydantic_model(schema, nested_model_name)
        return nested_model_name
    
    if json_type in type_mapping:
        return type_mapping[json_type]

    return "any"

def generate_function_stubs(openapi_spec: Dict[str, Any], namespace: str) -> str:
    namespace = normalize_namespace(namespace)
    paths = openapi_spec.get("paths", {})
    function_stubs = []
    namespace_prefix = namespace + '__'

    for path, methods in paths.items():
        for method, details in methods.items():
            function_name = f"{method}_{path.strip('/').replace('/', '_')}"
            request_body = details.get("requestBody", {})
            responses = details.get("responses", {})
            parameters = details.get("parameters", [])

            input_type = "None"
            return_type = "any"
            param_defs = []

            for param in parameters:
                param_name = param["name"]
                param_type = get_pydantic_type(param["schema"], param_name)
                param_defs.append(f"{param_name}: {param_type}")
                function_name = function_name.replace(f"{{{param_name}}}", param_name.upper())

            if request_body:
                content = request_body.get("content", {})
                if "application/json" in content:
                    schema_ref = content["application/json"]["schema"].get("$ref", "")
                    input_type = schema_ref.split('/')[-1]
                    param_defs.append(f"input: {input_type}")

            if '200' in responses or '201' in responses:
                response = responses.get('200', responses.get('201', {}))
                content = response.get("content", {})
                if "application/json" in content:
                    schema = content["application/json"]["schema"]
                    schema_ref = schema.get("$ref", "")
                    if schema.get("type") == "array":
                        item_ref = schema.get("items", {}).get("$ref", "")
                        if item_ref:
                            return_type = f"List[{item_ref.split('/')[-1]}]"
                    else:
                        return_type = schema_ref.split('/')[-1]

            full_function_name = namespace_prefix + function_name
            function_def = f"def {full_function_name}("
            function_def += ", ".join(param_defs)
            function_def += f") -> {return_type}:\n    pass\n"

            function_stubs.append(function_def)

    class_def = "class functions:\n"
    class_def += "\n".join([f"    {line}" for line in function_stubs])
    
    return class_def

# Example usage
openapi_spec = {
    "openapi": "3.0.1",
    "info": {
        "title": "Light API",
        "version": "v1"
    },
    "paths": {
        "/Light": {
            "get": {
                "tags": [
                    "Light"
                ],
                "summary": "Retrieves all lights in the system.",
                "operationId": "get_all_lights",
                "responses": {
                    "200": {
                        "description": "Success",
                        "content": {
                            "text/plain": {
                                "schema": {
                                    "type": "array",
                                    "items": {
                                        "$ref": "#/components/schemas/LightStateModel"
                                    }
                                }
                            },
                            "application/json": {
                                "schema": {
                                    "type": "array",
                                    "items": {
                                        "$ref": "#/components/schemas/LightStateModel"
                                    }
                                }
                            },
                            "text/json": {
                                "schema": {
                                    "type": "array",
                                    "items": {
                                        "$ref": "#/components/schemas/LightStateModel"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        },
        "/Light/{id}": {
            "get": {
                "tags": [
                    "Light"
                ],
                "summary": "Retrieves a specific light by its ID.",
                "operationId": "get_light",
                "parameters": [
                    {
                        "name": "id",
                        "in": "path",
                        "description": "The ID of the light from the get_all_lights tool.",
                        "required": True,
                        "style": "simple",
                        "schema": {
                            "type": "string"
                        }
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "content": {
                            "text/plain": {
                                "schema": {
                                    "$ref": "#/components/schemas/LightStateModel"
                                }
                            },
                            "application/json": {
                                "schema": {
                                    "$ref": "#/components/schemas/LightStateModel"
                                }
                            },
                            "text/json": {
                                "schema": {
                                    "$ref": "#/components/schemas/LightStateModel"
                                }
                            }
                        }
                    }
                }
            },
            "post": {
                "tags": [
                    "Light"
                ],
                "summary": "Changes the state of a light.",
                "operationId": "change_light_state",
                "parameters": [
                    {
                        "name": "id",
                        "in": "path",
                        "description": "The ID of the light to change from the get_all_lights tool.",
                        "required": True,
                        "style": "simple",
                        "schema": {
                            "type": "string"
                        }
                    }
                ],
                "requestBody": {
                    "description": "The new state of the light and change parameters.",
                    "content": {
                        "application/json": {
                            "schema": {
                                "$ref": "#/components/schemas/ChangeStateRequest"
                            }
                        },
                        "text/json": {
                            "schema": {
                                "$ref": "#/components/schemas/ChangeStateRequest"
                            }
                        },
                        "application/*+json": {
                            "schema": {
                                "$ref": "#/components/schemas/ChangeStateRequest"
                            }
                        }
                    }
                },
                "responses": {
                    "200": {
                        "description": "Success",
                        "content": {
                            "text/plain": {
                                "schema": {
                                    "$ref": "#/components/schemas/LightStateModel"
                                }
                            },
                            "application/json": {
                                "schema": {
                                    "$ref": "#/components/schemas/LightStateModel"
                                }
                            },
                            "text/json": {
                                "schema": {
                                    "$ref": "#/components/schemas/LightStateModel"
                                }
                            }
                        }
                    }
                }
            }
        }
    },
    "components": {
        "schemas": {
            "ChangeStateRequest": {
                "type": "object",
                "properties": {
                    "isOn": {
                        "type": "boolean",
                        "description": "Specifies whether the light is turned on or off.",
                        "nullable": True
                    },
                    "hexColor": {
                        "type": "string",
                        "description": "The hex color code for the light.",
                        "nullable": True
                    },
                    "brightness": {
                        "type": "integer",
                        "description": "The brightness level of the light.",
                        "format": "int32",
                        "nullable": True
                    },
                    "fadeDurationInMilliseconds": {
                        "type": "integer",
                        "description": "Duration for the light to fade to the new state, in milliseconds.",
                        "format": "int32",
                        "nullable": True
                    },
                    "scheduledTime": {
                        "type": "string",
                        "description": "The time at which the change should occur.",
                        "format": "date-time",
                        "nullable": True
                    }
                },
                "additionalProperties": False,
                "description": "Represents a request to change the state of the light."
            },
            "LightStateModel": {
                "type": "object",
                "properties": {
                    "id": {
                        "type": "string",
                        "nullable": True
                    },
                    "name": {
                        "type": "string",
                        "nullable": True
                    },
                    "on": {
                        "type": "boolean",
                        "nullable": True
                    },
                    "brightness": {
                        "type": "integer",
                        "format": "int32",
                        "nullable": True
                    },
                    "hexColor": {
                        "type": "string",
                        "nullable": True
                    }
                },
                "additionalProperties": False
            }
        }
    }
}

pydantic_models = parse_openapi_spec(openapi_spec, namespace="LightPlugin")
print(pydantic_models)

function_stubs = generate_function_stubs(openapi_spec, namespace="LightPlugin")
print(function_stubs)
