namespace Unosquare.Swan.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Attributes;

    /// <summary>
    /// Provides methods to parse command line arguments.
    /// Based on CommandLine (Copyright 2005-2015 Giacomo Stelluti Scala and Contributors.)
    /// </summary>
    /// <example>
    /// The following example shows how to parse CLI arguments into objects.
    /// <code>
    /// class Example 
    /// {
    ///     using System;
    ///     using Unosquare.Swan;
    ///     using Unosquare.Swan.Attributes;
    ///     
    ///     static void Main(string[] args)
    ///     {
    ///         // create an instance of the Options class
    ///         var options = new Options();
    ///         
    ///         // parse the supplied command-line arguments into the options object
    ///         var res = Runtime.ArgumentParser.ParseArguments(args, options);
    ///     }
    ///     
    ///     class Options
    ///     {
    ///         [ArgumentOption('v', "verbose", HelpText = "Set verbose mode.")]
    ///         public bool Verbose { get; set; }
    ///
    ///         [ArgumentOption('u', Required = true, HelpText = "Set user name.")]
    ///         public string Username { get; set; }
    ///
    ///         [ArgumentOption('n', "names", Separator = ',',
    ///         Required = true, HelpText = "A list of files separated by a comma")]
    ///         public string[] Files { get; set; }
    ///         
    ///         [ArgumentOption('p', "port", DefaultValue = 22, HelpText = "Set port.")]
    ///         public int Port { get; set; }
    ///
    ///         [ArgumentOption("color", DefaultValue = ConsoleColor.Red,
    ///         HelpText = "Set a color.")]
    ///         public ConsoleColor Color { get; set; }
    ///     }
    /// }
    /// </code>
    /// The following code describes how to parse CLI verbs.
    /// <code>
    /// class Example2 
    /// {
    ///     using Unosquare.Swan;
    ///     using Unosquare.Swan.Attributes;
    ///     
    ///     static void Main(string[] args)
    ///     {
    ///         // create an instance of the VerbOptions class
    ///         var options = new VerbOptions();
    ///         
    ///         // parse the supplied command-line arguments into the options object
    ///         var res = Runtime.ArgumentParser.ParseArguments(args, options);
    ///         
    ///         // if there were no errors parsing
    ///         if (res)
    ///         {
    ///             if(options.Run != null)
    ///             {
    ///                 // run verb was selected
    ///             }
    ///             
    ///             if(options.Print != null)
    ///             {
    ///                 // print verb was selected
    ///             }
    ///         }
    ///         
    ///         // flush all error messages
    ///         Terminal.Flush();
    ///     }
    ///     
    ///     class VerbOptions
    ///     {
    ///         [VerbOption("run", HelpText = "Run verb.")]
    ///         public RunVerbOption Run { get; set; }
    ///         
    ///         [VerbOption("print", HelpText = "Print verb.")]
    ///         public PrintVerbOption Print { get; set; }
    ///     }
    ///     
    ///     class RunVerbOption
    ///     {
    ///         [ArgumentOption('o', "outdir", HelpText = "Output directory",
    ///         DefaultValue = "", Required = false)]
    ///         public string OutDir { get; set; }
    ///     }
    ///     
    ///     class PrintVerbOption
    ///     {
    ///         [ArgumentOption('t', "text", HelpText = "Text to print",
    ///         DefaultValue = "", Required = false)]
    ///         public string Text { get; set; }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class ArgumentParser
    {
        private const char Dash = '-';

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentParser"/> class.
        /// </summary>
        public ArgumentParser()
            : this(new ArgumentParserSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentParser" /> class,
        /// configurable with <see cref="ArgumentParserSettings" /> using a delegate.
        /// </summary>
        /// <param name="parseSettings">The parse settings.</param>
        public ArgumentParser(ArgumentParserSettings parseSettings)
        {
            Settings = parseSettings ?? throw new ArgumentNullException(nameof(parseSettings));
        }

        /// <summary>
        /// Gets the instance that implements <see cref="ArgumentParserSettings" /> in use.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        public ArgumentParserSettings Settings { get; }

        /// <summary>
        /// Parses a string array of command line arguments constructing values in an instance of type <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of the options</typeparam>
        /// <param name="args">The arguments.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>
        /// <c>true</c> if was converted successfully; otherwise,  <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The exception that is thrown when a null reference (Nothing in Visual Basic) 
        /// is passed to a method that does not accept it as a valid argument
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The exception that is thrown when a method call is invalid for the object's current state
        /// </exception>
        public bool ParseArguments<T>(IEnumerable<string> args, T instance)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var properties = Runtime.PropertyTypeCache.RetrieveAllProperties<T>(true).ToArray();
            var verbName = string.Empty;

            if (properties.Any(x => x.GetCustomAttributes(typeof(VerbOptionAttribute), false).Any()))
            {
                var selectedVerb = !args.Any()
                    ? null
                    : properties.FirstOrDefault(x =>
                        Runtime.AttributeCache.RetrieveOne<VerbOptionAttribute>(x).Name.Equals(args.First()));

                if (selectedVerb == null)
                {
                    "No verb was specified".WriteLine(ConsoleColor.Red);
                    "Valid verbs:".WriteLine(ConsoleColor.Cyan);
                    properties.Select(x => Runtime.AttributeCache.RetrieveOne<VerbOptionAttribute>(x)).Where(x => x != null)
                        .Select(x => $"  {x.Name}\t\t{x.HelpText}")
                        .ToList()
                        .ForEach(x => x.WriteLine(ConsoleColor.Cyan));

                    return false;
                }

                verbName = selectedVerb.Name;
                if (instance.GetType().GetProperty(verbName).GetValue(instance) == null)
                {
                    var propertyInstance = Activator.CreateInstance(selectedVerb.PropertyType);
                    instance.GetType().GetProperty(verbName).SetValue(instance, propertyInstance);
                }

                properties = Runtime.PropertyTypeCache.RetrieveAllProperties(selectedVerb.PropertyType, true).ToArray();
            }

            if (properties.Any() == false)
                throw new InvalidOperationException($"Type {typeof(T).Name} is not valid");

            var requiredList = new List<string>();
            var updatedList = new List<PropertyInfo>();
            var unknownList = PopulateInstance(args, instance, properties, verbName, updatedList);

            foreach (var targetProperty in properties.Except(updatedList))
            {
                var defaultValue = Runtime.AttributeCache.RetrieveOne<ArgumentOptionAttribute>(targetProperty)?.DefaultValue;

                if (defaultValue == null)
                    continue;

                if (string.IsNullOrEmpty(verbName))
                {
                    SetPropertyValue(targetProperty, defaultValue.ToString(), instance);
                }
                else
                {
                    var property = instance.GetType().GetProperty(verbName);
                    if (SetPropertyValue(targetProperty, defaultValue.ToString(), property.GetValue(instance, null)))
                        updatedList.Add(targetProperty);
                }
            }

            foreach (var targetProperty in properties)
            {
                var optionAttr = Runtime.AttributeCache.RetrieveOne<ArgumentOptionAttribute>(targetProperty);

                if (optionAttr == null || optionAttr.Required == false)
                    continue;

                if (string.IsNullOrWhiteSpace(verbName))
                {
                    if (targetProperty.GetValue(instance) == null)
                    {
                        requiredList.Add(optionAttr.LongName ?? optionAttr.ShortName);
                    }
                }
                else
                {
                    var property = instance.GetType().GetProperty(verbName);

                    if (targetProperty.GetValue(property.GetValue(instance)) == null)
                    {
                        requiredList.Add(optionAttr.LongName ?? optionAttr.ShortName);
                    }
                }
            }

            if ((Settings.IgnoreUnknownArguments || !unknownList.Any()) && !requiredList.Any()) return true;

#if !NETSTANDARD1_3 && !UWP
            if (Settings.WriteBanner)
                Runtime.WriteWelcomeBanner();
#endif

            WriteUsage(properties);

            if (unknownList.Any())
                $"Unknown arguments: {string.Join(", ", unknownList)}".WriteLine(ConsoleColor.Red);

            if (requiredList.Any())
                $"Required arguments: {string.Join(", ", requiredList)}".WriteLine(ConsoleColor.Red);

            return false;
        }

        private List<string> PopulateInstance<T>(IEnumerable<string> args, T instance, PropertyInfo[] properties, string verbName, List<PropertyInfo> updatedList)
        {
            var unknownList = new List<string>();
            var propertyName = string.Empty;

            foreach (var arg in args)
            {
                if (string.IsNullOrWhiteSpace(propertyName) == false)
                {
                    var targetProperty = TryGetProperty(properties, propertyName);

                    // Skip if the property is not found
                    if (targetProperty == null)
                    {
                        unknownList.Add(propertyName);
                        propertyName = string.Empty;
                        continue;
                    }

                    if (string.IsNullOrEmpty(verbName))
                    {
                        if (SetPropertyValue(targetProperty, arg, instance))
                            updatedList.Add(targetProperty);
                    }
                    else
                    {
                        var property = instance.GetType().GetProperty(verbName);
                        instance.GetType().GetProperty(verbName);

                        if (SetPropertyValue(targetProperty, arg, property.GetValue(instance, null)))
                            updatedList.Add(targetProperty);
                    }

                    propertyName = string.Empty;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(arg) || arg[0] != Dash) continue;

                    propertyName = arg.Substring(1);
                    if (propertyName[0] == Dash) propertyName = propertyName.Substring(1);

                    var targetProperty = TryGetProperty(properties, propertyName);

                    // If the arg is a boolean property set it to true.
                    if (targetProperty == null || targetProperty.PropertyType != typeof(bool)) continue;

                    if (string.IsNullOrEmpty(verbName))
                    {
                        if (SetPropertyValue(targetProperty, true.ToString(), instance))
                            updatedList.Add(targetProperty);
                    }
                    else
                    {
                        var property = instance.GetType().GetProperty(verbName);
                        instance.GetType().GetProperty(verbName);

                        if (SetPropertyValue(targetProperty, true.ToString(), property.GetValue(instance, null)))
                            updatedList.Add(targetProperty);
                    }

                    propertyName = string.Empty;
                }
            }

            if (string.IsNullOrEmpty(propertyName) == false)
            {
                unknownList.Add(propertyName);
            }

            return unknownList;
        }

        private static void WriteUsage(IEnumerable<PropertyInfo> properties)
        {
            var options = properties.Select(p => Runtime.AttributeCache.RetrieveOne<ArgumentOptionAttribute>(p))
                .Where(x => x != null);

            foreach (var option in options)
            {
                string.Empty.WriteLine();

                // TODO: If Enum list values
                var shortName = string.IsNullOrWhiteSpace(option.ShortName) ? string.Empty : $"-{option.ShortName}";
                var longName = string.IsNullOrWhiteSpace(option.LongName) ? string.Empty : $"--{option.LongName}";
                var comma = string.IsNullOrWhiteSpace(shortName) || string.IsNullOrWhiteSpace(longName)
                    ? string.Empty
                    : ", ";
                var defaultValue = option.DefaultValue == null ? string.Empty : $"(Default: {option.DefaultValue}) ";

                $"  {shortName}{comma}{longName}\t\t{defaultValue}{option.HelpText}".WriteLine(ConsoleColor.Cyan);
            }

            string.Empty.WriteLine();
            "  --help\t\tDisplay this help screen.".WriteLine(ConsoleColor.Cyan);
        }

        private bool SetPropertyValue<T>(PropertyInfo targetProperty, string propertyValueString, T result)
        {
            var optionAttr = Runtime.AttributeCache.RetrieveOne<ArgumentOptionAttribute>(targetProperty);

            if (targetProperty.PropertyType.GetTypeInfo().IsEnum)
            {
                var parsedValue = Enum.Parse(
                    targetProperty.PropertyType,
                    propertyValueString,
                    Settings.CaseInsensitiveEnumValues);
                targetProperty.SetValue(result, Enum.ToObject(targetProperty.PropertyType, parsedValue));

                return true;
            }

            if (targetProperty.PropertyType.IsCollection())
            {
                var itemType = targetProperty.PropertyType.GetElementType();

                if (itemType == null)
                {
                    throw new InvalidOperationException(
                        $"The option collection {optionAttr.ShortName ?? optionAttr.LongName} should be an array");
                }

                var propertyArrayValue = propertyValueString.Split(optionAttr.Separator);
                var arr = Array.CreateInstance(itemType, propertyArrayValue.Cast<object>().Count());

                var i = 0;
                foreach (var value in propertyArrayValue)
                {
                    if (itemType.TryParseBasicType(value, out var itemvalue))
                        arr.SetValue(itemvalue, i++);
                }

                targetProperty.SetValue(result, arr);

                return true;
            }

            if (!targetProperty.PropertyType.TryParseBasicType(propertyValueString, out var propertyValue))
                return false;

            targetProperty.SetValue(result, propertyValue);
            return true;
        }

        private PropertyInfo TryGetProperty(IEnumerable<PropertyInfo> properties, string propertyName)
            => properties.FirstOrDefault(p =>
                string.Equals(Runtime.AttributeCache.RetrieveOne<ArgumentOptionAttribute>(p)?.LongName, propertyName, Settings.NameComparer) ||
                string.Equals(Runtime.AttributeCache.RetrieveOne<ArgumentOptionAttribute>(p)?.ShortName, propertyName, Settings.NameComparer));
    }
}
