# Code style
Summary:
- Event handler methods should say what they actually do
- `extern` DLLImports: pull in documentation from their READMEs or whatever
- `enum`s: should be singular for non-Flags and plural for Flags
- Description of what classes actually are for
- Reformat
- Use `LoggerHelper`
- `private` instance variables start with `_`
- Verify `_logger` scope
- `? ... : ...`: fix indent [(1)](#1)
- `blah\r\n.blah`: fix indent [(2)](#2)
- Remove useless `this.`
- `{ }` => `{\r\n}`
- Object initialization: fix indent [(3)](#3)
- Multiline method calls: fix indent [(4)](#4)
- Comments wrap at 80 characters, when possible

## `.editorconfig`
```ini
# Remove the line below if you want to inherit .editorconfig settings from higher directories
root = true

# C# files
[*.cs]

#### Core EditorConfig Options ####

# Indentation and spacing
indent_size = 4
indent_style = space
tab_width = 4

# New line preferences
end_of_line = crlf
insert_final_newline = false

#### .NET Code Actions ####

# Type members
dotnet_hide_advanced_members = false
dotnet_member_insertion_location = with_other_members_of_the_same_kind
dotnet_property_generation_behavior = prefer_throwing_properties

# Symbol search
dotnet_search_reference_assemblies = true

#### .NET Coding Conventions ####

# Organize usings
dotnet_separate_import_directive_groups = false
dotnet_sort_system_directives_first = false
file_header_template = unset

# this. and Me. preferences
dotnet_style_qualification_for_event = false:warning
dotnet_style_qualification_for_field = false
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_property = false:warning

# Language keywords vs BCL types preferences
dotnet_style_predefined_type_for_locals_parameters_members = true:warning
dotnet_style_predefined_type_for_member_access = true:warning

# Parentheses preferences
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:warning
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity:warning
dotnet_style_parentheses_in_other_operators = never_if_unnecessary:warning
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:warning

# Modifier preferences
dotnet_style_require_accessibility_modifiers = for_non_interface_members

# Expression-level preferences
dotnet_prefer_system_hash_code = true:warning
dotnet_style_coalesce_expression = true:warning
dotnet_style_collection_initializer = true:warning
dotnet_style_explicit_tuple_names = true:warning
dotnet_style_namespace_match_folder = true
dotnet_style_null_propagation = true:warning
dotnet_style_object_initializer = true:warning
dotnet_style_operator_placement_when_wrapping = beginning_of_line
dotnet_style_prefer_auto_properties = false:warning
dotnet_style_prefer_collection_expression = when_types_loosely_match
dotnet_style_prefer_compound_assignment = true:warning
dotnet_style_prefer_conditional_expression_over_assignment = true:warning
dotnet_style_prefer_conditional_expression_over_return = true:warning
dotnet_style_prefer_foreach_explicit_cast_in_source = when_strongly_typed
dotnet_style_prefer_inferred_anonymous_type_member_names = true:warning
dotnet_style_prefer_inferred_tuple_names = true:warning
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:warning
dotnet_style_prefer_simplified_boolean_expressions = true:warning
dotnet_style_prefer_simplified_interpolation = true

# Field preferences
dotnet_style_readonly_field = true:warning

# Parameter preferences
dotnet_code_quality_unused_parameters = all:warning

# Suppression preferences
dotnet_remove_unnecessary_suppression_exclusions = none

# New line preferences
dotnet_style_allow_multiple_blank_lines_experimental = false:warning
dotnet_style_allow_statement_immediately_after_block_experimental = false:warning

#### C# Coding Conventions ####

# var preferences
csharp_style_var_elsewhere = false:warning
csharp_style_var_for_built_in_types = false:warning
csharp_style_var_when_type_is_apparent = false:warning

# Expression-bodied members
csharp_style_expression_bodied_accessors = when_on_single_line:warning
csharp_style_expression_bodied_constructors = false:warning
csharp_style_expression_bodied_indexers = when_on_single_line:warning
csharp_style_expression_bodied_lambdas = when_on_single_line:warning
csharp_style_expression_bodied_local_functions = false:warning
csharp_style_expression_bodied_methods = when_on_single_line:warning
csharp_style_expression_bodied_operators = when_on_single_line:warning
csharp_style_expression_bodied_properties = when_on_single_line:warning

# Pattern matching preferences
csharp_style_pattern_matching_over_as_with_null_check = true:warning
csharp_style_pattern_matching_over_is_with_cast_check = true:warning
csharp_style_prefer_extended_property_pattern = true
csharp_style_prefer_not_pattern = true:warning
csharp_style_prefer_pattern_matching = true:warning
csharp_style_prefer_switch_expression = true:warning

# Null-checking preferences
csharp_style_conditional_delegate_call = true:warning

# Modifier preferences
csharp_prefer_static_anonymous_function = true:warning
csharp_prefer_static_local_function = true:warning
csharp_preferred_modifier_order = public,private,protected,internal,file,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,required,volatile,async
csharp_style_prefer_readonly_struct = true:warning
csharp_style_prefer_readonly_struct_member = true

# Code-block preferences
csharp_prefer_braces = true:warning
csharp_prefer_simple_using_statement = true:warning
csharp_prefer_system_threading_lock = true
csharp_style_namespace_declarations = block_scoped:warning
csharp_style_prefer_method_group_conversion = false:warning
csharp_style_prefer_primary_constructors = true
csharp_style_prefer_top_level_statements = false:warning

# Expression-level preferences
csharp_prefer_simple_default_expression = true:warning
csharp_style_deconstructed_variable_declaration = true:warning
csharp_style_implicit_object_creation_when_type_is_apparent = true:warning
csharp_style_inlined_variable_declaration = true:warning
csharp_style_prefer_implicitly_typed_lambda_expression = true
csharp_style_prefer_index_operator = true:warning
csharp_style_prefer_local_over_anonymous_function = true:warning
csharp_style_prefer_null_check_over_type_check = true:warning
csharp_style_prefer_range_operator = true:warning
csharp_style_prefer_tuple_swap = true:warning
csharp_style_prefer_unbound_generic_type_in_nameof = true
csharp_style_prefer_utf8_string_literals = true
csharp_style_throw_expression = true:warning
csharp_style_unused_value_assignment_preference = discard_variable:silent
csharp_style_unused_value_expression_statement_preference = discard_variable

# 'using' directive preferences
csharp_using_directive_placement = outside_namespace:warning

# New line preferences
csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental = true:warning
csharp_style_allow_blank_line_after_token_in_arrow_expression_clause_experimental = true:warning
csharp_style_allow_blank_line_after_token_in_conditional_expression_experimental = true:warning
csharp_style_allow_blank_lines_between_consecutive_braces_experimental = false:warning
csharp_style_allow_embedded_statements_on_same_line_experimental = true:warning

#### C# Formatting Rules ####

# New line preferences
csharp_new_line_before_catch = true
csharp_new_line_before_else = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_anonymous_types = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_open_brace = all
csharp_new_line_between_query_expression_clauses = true

# Indentation preferences
csharp_indent_block_contents = true
csharp_indent_braces = false
csharp_indent_case_contents = true
csharp_indent_case_contents_when_block = false
csharp_indent_labels = one_less_than_current
csharp_indent_switch_labels = true

# Space preferences
csharp_space_after_cast = false
csharp_space_after_colon_in_inheritance_clause = true
csharp_space_after_comma = true
csharp_space_after_dot = false
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_after_semicolon_in_for_statement = true
csharp_space_around_binary_operators = before_and_after
csharp_space_around_declaration_statements = false
csharp_space_before_colon_in_inheritance_clause = true
csharp_space_before_comma = false
csharp_space_before_dot = false
csharp_space_before_open_square_brackets = false
csharp_space_before_semicolon_in_for_statement = false
csharp_space_between_empty_square_brackets = false
csharp_space_between_method_call_empty_parameter_list_parentheses = false
csharp_space_between_method_call_name_and_opening_parenthesis = false
csharp_space_between_method_call_parameter_list_parentheses = false
csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
csharp_space_between_method_declaration_name_and_open_parenthesis = false
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_parentheses = false
csharp_space_between_square_brackets = false

# Wrapping preferences
csharp_preserve_single_line_blocks = true
csharp_preserve_single_line_statements = false

#### Naming styles ####

# Naming rules

dotnet_naming_rule.interface_should_be_begins_with_i.severity = warning
dotnet_naming_rule.interface_should_be_begins_with_i.symbols = interface
dotnet_naming_rule.interface_should_be_begins_with_i.style = begins_with_i

dotnet_naming_rule.types_should_be_pascal_case.severity = warning
dotnet_naming_rule.types_should_be_pascal_case.symbols = types
dotnet_naming_rule.types_should_be_pascal_case.style = pascal_case

dotnet_naming_rule.non_field_members_should_be_pascal_case.severity = warning
dotnet_naming_rule.non_field_members_should_be_pascal_case.symbols = non_field_members
dotnet_naming_rule.non_field_members_should_be_pascal_case.style = pascal_case

dotnet_naming_rule.public_members_should_be_pascal_case.severity = warning
dotnet_naming_rule.public_members_should_be_pascal_case.symbols = public_members
dotnet_naming_rule.public_members_should_be_pascal_case.style = pascal_case

dotnet_naming_rule.constant_field_should_be_pascal_case.severity = warning
dotnet_naming_rule.constant_field_should_be_pascal_case.symbols = constant_field
dotnet_naming_rule.constant_field_should_be_pascal_case.style = pascal_case

dotnet_naming_rule.non_public_static_field_should_be_begins_with_s_.severity = warning
dotnet_naming_rule.non_public_static_field_should_be_begins_with_s_.symbols = non_public_static_field
dotnet_naming_rule.non_public_static_field_should_be_begins_with_s_.style = begins_with_s_

dotnet_naming_rule.private_field_should_be_begins_with__.severity = warning
dotnet_naming_rule.private_field_should_be_begins_with__.symbols = private_field
dotnet_naming_rule.private_field_should_be_begins_with__.style = begins_with__

# Symbol specifications

dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_symbols.interface.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.interface.required_modifiers = 

dotnet_naming_symbols.types.applicable_kinds = class, struct, interface, enum
dotnet_naming_symbols.types.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.types.required_modifiers = 

dotnet_naming_symbols.non_field_members.applicable_kinds = property, event, method
dotnet_naming_symbols.non_field_members.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.non_field_members.required_modifiers = 

dotnet_naming_symbols.private_field.applicable_kinds = field
dotnet_naming_symbols.private_field.applicable_accessibilities = private
dotnet_naming_symbols.private_field.required_modifiers = 

dotnet_naming_symbols.constant_field.applicable_kinds = field
dotnet_naming_symbols.constant_field.applicable_accessibilities = *
dotnet_naming_symbols.constant_field.required_modifiers = const

dotnet_naming_symbols.non_public_static_field.applicable_kinds = field
dotnet_naming_symbols.non_public_static_field.applicable_accessibilities = internal, private, protected, protected_internal, private_protected, local
dotnet_naming_symbols.non_public_static_field.required_modifiers = static

dotnet_naming_symbols.public_members.applicable_kinds = property, field, event, delegate
dotnet_naming_symbols.public_members.applicable_accessibilities = public
dotnet_naming_symbols.public_members.required_modifiers = 

# Naming styles

dotnet_naming_style.pascal_case.required_prefix = 
dotnet_naming_style.pascal_case.required_suffix = 
dotnet_naming_style.pascal_case.word_separator = 
dotnet_naming_style.pascal_case.capitalization = pascal_case

dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.required_suffix = 
dotnet_naming_style.begins_with_i.word_separator = 
dotnet_naming_style.begins_with_i.capitalization = pascal_case

dotnet_naming_style.begins_with__.required_prefix = _
dotnet_naming_style.begins_with__.required_suffix = 
dotnet_naming_style.begins_with__.word_separator = 
dotnet_naming_style.begins_with__.capitalization = camel_case

dotnet_naming_style.begins_with_s_.required_prefix = s_
dotnet_naming_style.begins_with_s_.required_suffix = 
dotnet_naming_style.begins_with_s_.word_separator = 
dotnet_naming_style.begins_with_s_.capitalization = camel_case
```

## (1)
Example:

```cs
// Determine if a type or property and extract the relevant attribute
// provider
ICustomAttributeProvider? attributeProvider = context.PropertyInfo is not null
    ? context.PropertyInfo.AttributeProvider
    : context.TypeInfo.Type;
```

## (2)
Example:

```cs
// Look up any default string attributes
DefaultStringAttribute? defaultStringAttribute = attributeProvider?
    .GetCustomAttributes(inherit: true)
    .Select(attr => attr as DefaultStringAttribute)
    .FirstOrDefault(attr => attr is not null);
```

## (3)
Example:

```cs
[Description("Launcher entries.")]
public List<LauncherEntry> LauncherEntries { get; set; } =
[
    new()
    {
        Name = "Windows Terminal",
        Command = "wt",
        IconColor = ColorEnum.Overlay0,
        Icon = ""
    },
    new()
    {
        Name = "File Explorer",
        Command = "explorer",
        CommandArguments =
        [
            "%USERPROFILE%"
        ]
    }
];
```

## (4)
Example:

```cs
JsonSerializer.Serialize(
    streamWriter.BaseStream,
    config,
    JsonSerializerOptions
);
```
