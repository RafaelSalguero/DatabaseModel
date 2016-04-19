using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DatabaseSchemaReader;
using DatabaseSchemaReader.CodeGen;
using DatabaseSchemaReader.DataSchema;
using Tonic;
using Tonic.MVVM;
using Tonic.MVVM.Extensions;

namespace DatabaseModel
{
    public class MainViewModel : BaseViewModel
    {
        public MainViewModel()
        {
            AddExtension(new CommandsExtension(this));

            this.Code = new ObservableCollection<CodeViewModel>();
            this.CS = Newtonsoft.Json.JsonConvert.DeserializeObject<ConnectionStringViewModel>(Properties.Settings.Default.CSV) ?? new ConnectionStringViewModel();

            ((INotifyPropertyChanged)this.CS).PropertyChanged += delegate
                  {
                      Properties.Settings.Default.CSV = Newtonsoft.Json.JsonConvert.SerializeObject(CS);
                      Properties.Settings.Default.Save();
                  };
        }


        public ConnectionStringViewModel CS
        {
            get; private set;
        }

        public string Table
        {
            get
            {
                return Properties.Settings.Default.Table;
            }
            set
            {
                Properties.Settings.Default.Table = value;
                Properties.Settings.Default.Save();

                UpdateTable();

            }
        }

        private async Task UpdateTable()
        {

            try
            {
                var dbReader = await Task.Run(() => new DatabaseReader(CS.ConnectionString, CS.ProviderName));
                var schema = await Task.Run(() => dbReader.ReadAll());

                var B = new StringBuilder();
                foreach (var T in TableSearch(schema.Tables, Table))
                {
                    B.AppendLine(await Task.Run(() => schema.FindTableByName(T).Name));
                }

                var C = await Task.Run(() => new CodeViewModel("Tables", B.ToString()));
                C.IsExpanded = true;
                Code.Clear();
                Code.Add(C);
            }
            catch (Exception ex)
            {
                Code.Clear();
                Code.Add(new CodeViewModel("Error", "No se pudo conectar a la base de datos: \n\r" +
                    ex.GetAllExceptions().Select(x => x.GetType().Name + ":" + x.Message).Aggregate("", (a, b) => "\n\r" + a + " -> " + b, x => x)
                    ));
            }

        }

        public string DomainNamespace
        {
            get
            {
                return Properties.Settings.Default.DomainNamespace;
            }
            set
            {
                Properties.Settings.Default.DomainNamespace = value;
                Properties.Settings.Default.Save();
            }
        }

        public string ModelNamespace
        {
            get
            {
                return Properties.Settings.Default.ModelNamespace;
            }
            set
            {
                Properties.Settings.Default.ModelNamespace = value;
                Properties.Settings.Default.Save();
            }
        }

        public string MetadataNamespace
        {
            get
            {
                return Properties.Settings.Default.MetadataNamespace;
            }
            set
            {
                Properties.Settings.Default.MetadataNamespace = value;
                Properties.Settings.Default.Save();
            }
        }

        public int DependencyLevel
        {
            get
            {
                return Properties.Settings.Default.DepLevel;
            }
            set
            {
                Properties.Settings.Default.DepLevel = value;
                Properties.Settings.Default.Save();
            }
        }

        public bool Metadata
        {
            get
            {
                return Properties.Settings.Default.Metadata;
            }
            set
            {
                Properties.Settings.Default.Metadata = value;
                Properties.Settings.Default.Save();
            }
        }

        public bool IgnoreLastDependency
        {
            get
            {
                return Properties.Settings.Default.IgnoreLastDependency;
            }
            set
            {
                Properties.Settings.Default.IgnoreLastDependency = value;
                Properties.Settings.Default.Save();
            }
        }

        public bool AddInterface
        {
            get
            {
                return Properties.Settings.Default.AddInterface;
            }
            set
            {
                Properties.Settings.Default.AddInterface = value;
                Properties.Settings.Default.Save();
            }
        }

        public bool AllTables
        {
            get
            {
                return Properties.Settings.Default.AllTables;
            }
            set
            {
                Properties.Settings.Default.AllTables = value;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }


        public ObservableCollection<CodeViewModel> Code
        {
            get;
            private set;
        }


        private void GeneratePartials(DatabaseTable Table, ICollection<CodeViewModel> ViewModels, string ModelNamespace, string MetadataNamespace)
        {
            //      [MetadataType(typeof(NeoData.Metadata.Agente))]
            //public partial class Agente { }
            var B = new StringBuilder();
            B.AppendLine($"[MetadataType(typeof({MetadataNamespace}.{Table.Name}))]");
            B.AppendLine($"public partial class {Table.Name} {{ }}");

            var CC = new CodeViewModel("Partials", B.ToString());
            ViewModels.Add(CC);
        }

        private static string GetForeignKeyVarName(string name)
        {
            if (name.ToLowerInvariant().StartsWith("__"))
                return name.Substring(2);
            else
                return name;
        }


        private static string GetForeignPropName(string name)
        {
            return "__" + name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="ViewModels"></param>
        /// <param name="Namer"></param>
        /// <param name="ModelNamespace"></param>
        /// <param name="MetadataNamespace"></param>
        /// <param name="dependencies"></param>
        /// <param name="tableDependencies"></param>
        /// <param name="lastDependency">Si es true se ignoraran todas las dependencias de esta tabla que esten afuera de las colección doneTables</param>
        /// <param name="doneTables">Si lastDependency es true, implica todas las tablas que van a ser incluidas en el modelo, de tal manera que las que no esten no seran generadas</param>
        /// <param name="metadata"></param>
        /// <param name="AddInterface">True para generar una interfaz con todas las columnas de la tabla sin incluir las propiedades de navegación para la entidad</param>
        /// <returns></returns>
        private static string GenerateEFModel(DatabaseTable Table, ICollection<CodeViewModel> ViewModels, INamer Namer,
            string ModelNamespace, string MetadataNamespace, bool dependencies, HashSet<string> tableDependencies, bool lastDependency, HashSet<string> doneTables,
            bool metadata, bool AddInterface)
        {
            var B = new StringBuilder();
            var Co = new StringBuilder();
            var FCh = new StringBuilder();
            var Cons = new StringBuilder();

            var Att = new StringBuilder();
            var AttExcludes = new StringBuilder();
            var AttProps = new StringBuilder();

            var ForeignMo = new StringBuilder();

            var Interface = new StringBuilder();

            Co.Clear();
            Co.AppendLine($"public virtual DbSet<{Table.Name}> {Table.Name} {{ get; set; }}");
            var Collection = new CodeViewModel($"DbSet", Co.ToString());
            ViewModels.Add(Collection);

            {
                foreach (var C in Table.Columns)
                {
                    if (C.IsPrimaryKey && C.DataType?.NetDataType == "System.Guid")
                    {
                        Cons.AppendLine($"          {C.Name} = Guid.NewGuid();");
                    }

                }
                if (!dependencies)

                    foreach (var FK in Table.ForeignKeyChildren)
                    {
                        if (FK.IsManyToManyTable())
                        {
                            var Other = FK.ManyToManyTraversal(Table);
                            var propertyName = Other.Name;
                            if (lastDependency && !doneTables.Contains(propertyName))
                                break;

                            tableDependencies.Add(Other.Name);

                            Cons.AppendLine($"          {propertyName} = new HashSet<{Other.Name}>();");

                            FCh.AppendLine("     [JsonIgnore]");
                            FCh.AppendLine($"     public virtual ICollection<{Other.Name}> {propertyName} {{ get; set; }}");
                            FCh.AppendLine();


                            var MM = new StringBuilder();

                            MM.AppendLine("     [JsonIgnore]");
                            MM.AppendLine($"public virtual ICollection<{Table.Name}> {Table.Name} {{ get; set; }}");
                            Collection = new CodeViewModel($"{Other.Name} property", MM.ToString());
                            ViewModels.Add(Collection);

                            MM.Clear();
                            MM.AppendLine($"{Table.Name} = new HashSet<{Table.Name}>();");
                            Collection = new CodeViewModel($"{Other.Name} constructor", MM.ToString());
                            ViewModels.Add(Collection);



                            MM.Clear();


                            MM.AppendLine($"modelBuilder.Entity<{Table.Name}>()");
                            MM.AppendLine($".HasMany(e => e.{propertyName})");
                            MM.AppendLine($".WithMany(e => e.{Table.Name})");
                            MM.AppendLine(".Map(c =>");
                            MM.AppendLine("{");
                            MM.AppendLine($"     c.MapLeftKey(\"Id{Table.Name}\");");
                            MM.AppendLine($"     c.MapRightKey(\"Id{Other.Name}\");");
                            MM.AppendLine("});");



                            Collection = new CodeViewModel($"{Table.Name} fluent", MM.ToString());
                            ViewModels.Add(Collection);
                        }
                        else
                        {
                            IEnumerable<DatabaseConstraint> fks = Table.InverseForeignKeys(FK);
                            if (lastDependency)
                                fks = fks.Where(x => doneTables.Contains(x.TableName));

                            var propNamesCount = new Dictionary<string, int>();
                            foreach (var fk in fks)
                            {
                                tableDependencies.Add(FK.Name);
                                var propertyName2 = FK.Name;

                                int Count = 0;
                                if (!propNamesCount.TryGetValue(propertyName2, out Count))
                                {
                                    propNamesCount.Add(propertyName2, Count);
                                }
                                propNamesCount[propertyName2]++;
                                if (Count > 0)
                                    propertyName2 += Count.ToString();


                                Cons.AppendLine($"          {propertyName2} = new HashSet<{FK.Name}>();");
                                FCh.AppendLine("     [JsonIgnore]");

                                var InversePropName = fk.Columns.Count == 1 ? fk.Columns[0] : fk.RefersToTable;
                                FCh.AppendLine($"     [InverseProperty(nameof({ModelNamespace}.{FK.Name}.{GetForeignPropName(InversePropName)}))]");
                                FCh.AppendLine($"     public virtual ICollection<{FK.Name}> {propertyName2} {{ get; set; }}");
                                FCh.AppendLine();
                            }
                        }
                    }
            };






            B.AppendLine("using System;");
            B.AppendLine("using System.Collections.Generic;");
            B.AppendLine("using System.ComponentModel.DataAnnotations;");
            B.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            B.AppendLine("using Newtonsoft.Json;");
            B.AppendLine();

            B.AppendLine($"namespace {ModelNamespace}");
            B.AppendLine("{");



            B.AppendLine($"[Table(\"{Table.Name}\")]");
            if (AddInterface)
                B.AppendLine($"public partial class {Table.Name} : I{Table.Name}");
            else
                B.AppendLine($"public partial class {Table.Name}");

            B.AppendLine("{");


            B.AppendLine($"     public {Table.Name}()");
            B.AppendLine("     {");
            B.Append(Cons.ToString());
            B.AppendLine("     }");
            B.AppendLine();

            var TableCollections = new Dictionary<string, List<string>>();
            Func<string, string, string> GetTableCollection = (table, collection) =>
           {
               List<string> collections;
               if (!TableCollections.TryGetValue(table, out collections))
               {
                   collections = new List<string>();
                   TableCollections.Add(table, collections);
               }

               if (collections.Count == 0)
               {
                   collections.Add(collection);
                   return collection;
               }
               else
               {
                   var s = $"{collection}{collections.Count}";
                   collections.Add(s);
                   return s;
               }
           };


            ////Recorre todas las llaves foraneas, incluyendo las compuestas:
            //foreach (var Fk in Table.ForeignKeys)
            //{
            //    if (Fk.Columns.Any(x => Table.Columns.First(col => col.Name == x).Nullable))
            //        B.AppendLine("     [Required]");


            //    B.AppendLine("     [JsonIgnore]");
            //    B.AppendLine($"     [ForeignKey(nameof({ModelNamespace}.{Table.Name}.{GetForeignKeyVarName(Ci.Name)}))]");
            //    B.AppendLine($"     public virtual {Ci.ForeignKeyTableName} { GetForeignPropName(Ci.Name)} {{ get; set; }}");
            //    B.AppendLine();
            //}

            foreach (var Ci in Table.Columns)
            {
                if (Ci.IsPrimaryKey)
                {
                    B.AppendLine("     [Key]");
                    if (Table.PrimaryKey.Columns.Count > 1)
                    {
                        var n = Table.PrimaryKey.Columns.IndexOf(Ci.Name);
                        B.AppendLine($"     [Column(Order = {n + 1})] ");
                    }
                }


                if (Ci.DbDataType == "bpchar" || Ci.DataType?.NetDataTypeCSharpName == "string")
                {
                    if (!Ci.Nullable)
                        B.AppendLine("     [Required]");
                    if (Ci.Length.HasValue && Ci.Length.Value > -1)
                        B.AppendLine($"     [StringLength({Ci.Length.Value})]");
                }




                var ask = (Ci.Nullable && !((Ci.DbDataType == "bpchar" || Ci.DataType != null && Ci.DataType.IsString) || Ci.DbDataType == "geography")) ? "?" : "";



                string NetDataType;
                if (Ci.DataType == null)
                {
                    switch (Ci.DbDataType)
                    {
                        case "geography":
                            if (!Ci.Nullable)
                                B.AppendLine("     [Required]"); ;
                            NetDataType = "System.Data.Spatial.DbGeography";
                            break;
                        case "bpchar":
                            NetDataType = "string";
                            break;
                        default:
                            throw new ArgumentException($"Type '{Ci.DbDataType}' not found");
                    }
                }
                else
                {
                    NetDataType = Ci.DataType.NetDataTypeCSharpName;
                    if (NetDataType == "System.Guid")
                        NetDataType = "Guid";
                }
                {
                    string Property;
                    if (Ci.IsForeignKey)
                    {
                        if (GetForeignKeyVarName(Ci.Name) != Ci.Name)
                            B.AppendLine($"     [Column(\"{Ci.Name}\")]");
                        Property = $"{NetDataType }{ask} { GetForeignKeyVarName(Ci.Name)}";
                    }
                    else
                    {
                        Property = $"{NetDataType }{ask} {(Ci.Name)}";
                    }

                    B.AppendLine($"     public {Property} {{ get; set; }}");
                    Interface.AppendLine($"      {Property} {{ get; set; }}");
                }
                B.AppendLine();
            }

            foreach (var Fk in Table.ForeignKeys.Where(x => x.RefersToTable != ""))
            {

                var FirstColumnName = Fk.Columns[0];

                {
                    B.AppendLine("     [JsonIgnore]");

                    string Names = Fk.Columns.Select(x => $"nameof({ModelNamespace}.{Table.Name}.{x})").Aggregate("", (a, b) => a == "" ? b : (a + Environment.NewLine + " + \", \" + " + b), x => x);

                    B.AppendLine($"     [ForeignKey({Names})]");
                    if (Fk.Columns.Count == 1)
                        B.AppendLine($"     public virtual {Fk.RefersToTable} {GetForeignPropName(FirstColumnName)} {{ get; set; }}");
                    else
                        B.AppendLine($"     public virtual {Fk.RefersToTable} {GetForeignPropName(Fk.RefersToTable)} {{ get; set; }}");

                    B.AppendLine();

                    tableDependencies.Add(Fk.RefersToTable);

                }
            }


            B.Append(FCh.ToString());

            B.AppendLine("}");
            B.AppendLine();

            if (AddInterface)
            {
                B.AppendLine($"public interface I{Table.Name}");
                B.AppendLine("{");
                B.AppendLine(Interface.ToString());
                B.AppendLine("}");
            }

            B.AppendLine("}");

            if (metadata)
            {
                Att.AppendLine("using Tonic.UI.Scaffold.Attributes;");
                Att.AppendLine("using System.ComponentModel;");
                Att.AppendLine("using System.ComponentModel.DataAnnotations;");
                Att.AppendLine();
                Att.AppendLine($"namespace {MetadataNamespace}");
                Att.AppendLine("{");

                Att.AppendLine();
                Att.AppendLine($"class {Table.Name}");
                Att.AppendLine("{");
                Att.AppendLine("#region Excludes");
                Att.AppendLine();
                Att.Append(AttExcludes);

                Att.AppendLine();

                Att.AppendLine("#endregion");
                Att.AppendLine();

                Att.Append(AttProps.ToString());
                Att.AppendLine("}");
                Att.AppendLine();
                Att.AppendLine("}");



                Collection = new CodeViewModel("Metadata", Att.ToString());
                ViewModels.Add(Collection);
            }
            return B.ToString();
        }

        public static IEnumerable<string> TableSearch(IEnumerable<DatabaseTable> Tables, string Text)
        {
            var TableName = Tables
                       .Select(x => x.Name)
                       .Where(x => x.ToLowerInvariant().Contains(Text.ToLowerInvariant()))
                       .OrderBy(x => Compute(x.ToLowerInvariant(), Text.ToLowerInvariant()));

            if (TableName.Any() == false)
            {
                TableName = Tables
                         .Select(x => x.Name)
                         .OrderBy(x => Compute(x.ToLowerInvariant(), Text.ToLowerInvariant()));
            }

            return TableName;
        }

        private void generateDep(int depLevel, DatabaseTable Table, DatabaseSchema schema, HashSet<string> doneTables, bool metadata)
        {
            if (doneTables.Contains(Table.Name)) return;

            doneTables.Add(Table.Name);
            var Settings = new CodeWriterSettings { CodeTarget = CodeTarget.PocoEntityCodeFirst };
            var Namer = Settings.Namer;

            HashSet<string> deps = new HashSet<string>();
            Code.Insert(0, new CodeViewModel($"{Table.Name}.cs", GenerateEFModel(Table, Code, Namer, DomainNamespace + "." + ModelNamespace, DomainNamespace + "." + MetadataNamespace, false, deps, depLevel == 0 && IgnoreLastDependency, doneTables, metadata, this.AddInterface), true));
            if (metadata)
                GeneratePartials(Table, Code, DomainNamespace + "." + ModelNamespace, DomainNamespace + "." + MetadataNamespace);

            Code.Insert(0, new CodeViewModel("names", Table.Name + "\n\r"));

            if (depLevel > 0)
            {
                foreach (var d in deps)
                {
                    generateDep(depLevel - 1, schema.FindTableByName(d), schema, doneTables, metadata);
                }
            }
        }

        public void Save()
        {
            var D = new System.Windows.Forms.FolderBrowserDialog();
            D.SelectedPath = Properties.Settings.Default.SaveFolder;
            if (D.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Properties.Settings.Default.SaveFolder = D.SelectedPath;
                Properties.Settings.Default.Save();

                var Folder = D.SelectedPath;
                foreach (var f in Code.Where(x => x.IsFile))
                {
                    var FilePath = System.IO.Path.Combine(Folder, f.Title);
                    System.IO.File.WriteAllText(FilePath, f.Code);
                }
            }
        }

        public void Generate()
        {
            try
            {
                var dbReader = new DatabaseReader(CS.ConnectionString, CS.ProviderName);
                var schema = dbReader.ReadAll();
                Code.Clear();

                if (AllTables)
                {
                    var H = new HashSet<string>();
                    foreach (var T in schema.Tables.Where(x => !(x.Name.StartsWith("pg_") || x.Name.StartsWith("sql_"))))
                    {
                        generateDep(0, T, schema, H, Metadata);
                    }
                }
                else
                {
                    int minDistance = int.MaxValue;
                    var TableName = TableSearch(schema.Tables, this.Table).First();

                    var Table = schema.FindTableByName(TableName);

                    var Settings = new CodeWriterSettings { CodeTarget = CodeTarget.PocoEntityCodeFirst };
                    var Namer = Settings.Namer;

                    if (Table == null)
                    {

                    }
                    else
                    {
                        generateDep(DependencyLevel, Table, schema, new HashSet<string>(), Metadata);
                    }
                }

                var filter = Code.GroupBy(x => x.Title).Select(x => new CodeViewModel(x.Key, x.Select(y => y.Code).Aggregate((a, b) => a + b), x.First().IsFile)).ToArray();
                Code.Clear();
                foreach (var F in filter)
                    Code.Add(F);


            }
            catch (Exception ex)
            {
                Code.Clear();
                Code.Add(new CodeViewModel("Error",
                    ex.GetAllExceptions().Select(x => x.GetType().Name + ":" + x.Message).Aggregate("", (a, b) => "\n\r" + a + " -> " + b, x => x)
                    ));
            }
        }

    }
}
