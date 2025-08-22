using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using YamlDotNet.Serialization;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace JsonConverter
{
    public partial class MainWindow : Window
    {
        private string currentFilePath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            cmbFileType.SelectionChanged += CmbFileType_SelectionChanged;
        }

        private async void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers supportés (*.json;*.xml;*.yaml;*.yml;*.csv)|*.json;*.xml;*.yaml;*.yml;*.csv|Tous les fichiers (*.*)|*.*",
                Title = "Ouvrir un fichier"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var loadingWindow = new LoadingWindow();
                try
                {
                    // Afficher la fenêtre de chargement
                    loadingWindow.Show();
                    Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            currentFilePath = openFileDialog.FileName;
                            txtFileInfo.Text = Path.GetFileName(currentFilePath);
                            LoadFile(currentFilePath);
                        });
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du chargement du fichier : {ex.Message}", 
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    loadingWindow.Close();
                }
            }
        }

        private async void LoadFile(string filePath)
        {
            var loadingWindow = new LoadingWindow();
            try
            {
                // Afficher la fenêtre de chargement
                loadingWindow.Show();
                Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                await System.Threading.Tasks.Task.Run(() =>
                {
                    string fileContent = File.ReadAllText(filePath);
                    Dispatcher.Invoke(() =>
                    {
                        txtRawContent.Text = fileContent;
                        string selectedType = ((ComboBoxItem)cmbFileType.SelectedItem).Content.ToString();
                        
                        switch (selectedType)
                        {
                            case "JSON":
                                DisplayJson(fileContent);
                                break;
                            case "XML":
                                DisplayXml(fileContent);
                                break;
                            case "YAML":
                                DisplayYaml(fileContent);
                                break;
                            case "CSV":
                                DisplayCsv(fileContent);
                                break;
                        }
                        txtStatus.Text = "Fichier chargé avec succès";
                    });
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = $"Erreur : {ex.Message}";
                    treeViewFormatted.Items.Clear();
                });
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    loadingWindow.Close();
                });
            }
        }

        private void CmbFileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentFilePath))
            {
                try
                {
                    LoadFile(currentFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du changement de type de fichier : {ex.Message}", 
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DisplayJson(string jsonContent)
        {
            try
            {
                var jsonObject = JToken.Parse(jsonContent);
                treeViewFormatted.Items.Clear();
                BuildTreeView(jsonObject, treeViewFormatted.Items);
            }
            catch (JsonReaderException)
            {
                throw new InvalidOperationException("Le contenu n'est pas un JSON valide.");
            }
        }

        private void DisplayXml(string xmlContent)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);
                treeViewFormatted.Items.Clear();
                BuildXmlTreeView(xmlDoc.DocumentElement, treeViewFormatted.Items);
            }
            catch (XmlException)
            {
                throw new InvalidOperationException("Le contenu n'est pas un XML valide.");
            }
        }

        private void DisplayYaml(string yamlContent)
        {
            try
            {
                var deserializer = new DeserializerBuilder().Build();
                var yamlObject = deserializer.Deserialize<object>(yamlContent);
                var json = JsonConvert.SerializeObject(yamlObject, Newtonsoft.Json.Formatting.Indented);
                var jsonObject = JToken.Parse(json);
                
                treeViewFormatted.Items.Clear();
                BuildTreeView(jsonObject, treeViewFormatted.Items);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Le contenu n'est pas un YAML valide.");
            }
        }

        private void DisplayCsv(string csvContent)
        {
            try
            {
                treeViewFormatted.Items.Clear();
                var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length == 0) return;

                // Utiliser la première ligne comme en-têtes de colonnes
                var headers = lines[0].Split(',');
                
                for (int i = 1; i < lines.Length; i++)
                {
                    var values = ParseCsvLine(lines[i]);
                    var item = new TreeViewItem { Header = $"Ligne {i}" };
                    
                    for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
                    {
                        item.Items.Add(new TreeViewItem { Header = $"{headers[j]}: {values[j]}" });
                    }
                    
                    treeViewFormatted.Items.Add(item);
                }
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Erreur lors de la lecture du fichier CSV.");
            }
        }

        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var inQuotes = false;
            var currentValue = new StringBuilder();
            
            for (int i = 0; i < line.Length; i++)
            {
                char currentChar = line[i];
                
                if (currentChar == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                
                if (currentChar == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString().Trim());
                    currentValue.Clear();
                    continue;
                }
                
                currentValue.Append(currentChar);
            }
            
            values.Add(currentValue.ToString().Trim());
            return values.ToArray();
        }

        private void BuildTreeView(JToken token, ItemCollection items)
        {
            if (token == null) return;

            if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    var item = new TreeViewItem { Header = property.Name };
                    items.Add(item);
                    BuildTreeView(property.Value, item.Items);
                }
            }
            else if (token is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    var item = new TreeViewItem { Header = $"[{i}]" };
                    items.Add(item);
                    BuildTreeView(array[i], item.Items);
                }
            }
            else
            {
                var value = token.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    items.Add(new TreeViewItem { Header = value });
                }
            }
        }

        private void BuildXmlTreeView(XmlNode node, ItemCollection items)
        {
            if (node == null) return;

            if (node.NodeType == XmlNodeType.Element)
            {
                var element = (XmlElement)node;
                var item = new TreeViewItem { Header = element.Name };
                
                // Ajouter les attributs
                foreach (XmlAttribute attr in element.Attributes)
                {
                    item.Items.Add(new TreeViewItem { Header = $"@{attr.Name} = {attr.Value}" });
                }
                
                // Ajouter les nœuds enfants
                if (element.HasChildNodes)
                {
                    foreach (XmlNode child in element.ChildNodes)
                        BuildXmlTreeView(child, item.Items);
                }
                
                items.Add(item);
            }
            else if (node.NodeType == XmlNodeType.Text)
            {
                var text = node.InnerText.Trim();
                if (!string.IsNullOrEmpty(text))
                    items.Add(new TreeViewItem { Header = text });
            }
        }

        private void TreeViewFormatted_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (treeViewFormatted.SelectedItem is TreeViewItem selectedItem)
            {
                try
                {
                    if (selectedItem.Header is string header)
                    {
                        Clipboard.SetText(header);
                        txtStatus.Text = "Valeur copiée dans le presse-papiers";
                    }
                }
                catch (Exception ex)
                {
                    txtStatus.Text = $"Erreur lors de la copie : {ex.Message}";
                }
            }
        }
    }
}
