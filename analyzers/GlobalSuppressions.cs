// This file is shared across all projects via Directory.Build.props.
//
// Only formatting-only rules and deliberate domain-vocabulary choices are suppressed here.
// Correctness rules stay on: if the build fails on one, fix the code rather than adding a line.
using System.Diagnostics.CodeAnalysis;

// --- File and header layout: this codebase has no file headers and no per-member XML docs ---
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File should have header", Justification = "No file headers in this codebase")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1636:File header copyright text should match", Justification = "No file headers in this codebase")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Comments are written where they explain something, not on every member")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "Comments are written where they explain something, not on every member")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:Property summary documentation should match accessors", Justification = "Summaries say what a property means, not 'Gets or sets'")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1629:Documentation text should end with a period", Justification = "Summaries are fragments, not sentences")]

// --- Ordering and spacing: members are grouped by feature, and the .editorconfig owns whitespace ---
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1200:Using directives should be placed correctly", Justification = "Usings sit outside the namespace, which stylecop.json configures")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Members are grouped by feature rather than by kind")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Members are grouped by feature rather than by accessibility")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Members are grouped by feature rather than by kind")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "Contradicts the IDE0009 style this codebase uses")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1111:Closing parenthesis should be on line of last parameter", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1127:Generic type constraints should be on their own line", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:Field names should not begin with underscore", Justification = "Private fields are _camelCase here")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1208:System using directives should be placed before other using directives", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1210:Using directives should be ordered alphabetically by namespace", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:Closing parenthesis should be spaced correctly", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1117:Parameters should be on same line or separate lines", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1005:Single line comment should begin with a space", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1500:Braces for multi-line statements should not share line", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:Closing brace should be followed by blank line", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1508:A closing brace should not be preceded by a blank line", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1512:Single-line comments should not be followed by blank line", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1025:Code should not contain multiple whitespace in a row", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1028:Code should not contain trailing whitespace", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1503:Braces should not be omitted", Justification = "Covered by the .editorconfig brace rules")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1515:Single-line comment should be preceded by blank line", Justification = "Formatting, owned by .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1413:Use trailing comma in multi-line initializers", Justification = "Not the house style")]

// --- Naming conventions this codebase has settled on ---
// `s_` on private static fields is the .NET runtime convention and what the .editorconfig asks for.
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1308:Variable names should not be prefixed", Justification = "s_ prefix on private statics matches the .editorconfig")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:Static readonly fields should begin with upper-case letter", Justification = "s_ prefix on private statics matches the .editorconfig")]

// --- Domain vocabulary. These names come from D&D 5E and the API contract; renaming them to
// --- satisfy an analyzer would make the code harder to read against the rulebook.
[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Class is a D&D class and Error is the result type; both are domain vocabulary")]
[assembly: SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "INT is the D&D abbreviation for Intelligence, not the integer type")]
[assembly: SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Response<T>, FilterRequest<T> and FilterOptions<T> expose static factories deliberately")]
