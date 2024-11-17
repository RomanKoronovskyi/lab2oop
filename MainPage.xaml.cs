using Microsoft.Maui.Storage;
using System.Xml.Linq;
using System.Xml.Xsl;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace lab2;

public partial class MainPage : ContentPage
{
    private string _selectedFilePath;
    private XDocument _xmlDocument;
    private IXmlParserStrategy _currentParserStrategy;

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnChooseFileClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".xml" } }
                }),
                PickerTitle = "Виберіть XML файл"
            });

            if (result != null && result.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                _selectedFilePath = result.FullPath;
                _xmlDocument = XDocument.Load(_selectedFilePath);
                DomButton.IsVisible = true;
                SaxButton.IsVisible = true;
                LinqButton.IsVisible = true;
            }
            else
            {
                OutputLabel.Text = "Оберіть XML файл.";
            }
        }
        catch (Exception ex)
        {
            OutputLabel.Text = $"Помилка при завантаженні файла: {ex.Message}";
        }
    }

    private async void OnInformationClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Інформація", "Оберіть XML-файл. Виберіть критерії пошуку, спосіб обробки документа та натисніть кнопку 'Пошук'. Також Ви можете трансформувати данний файл в HTML.", "OK");
    }

    private async void OnExitClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Підтвердження", "Ви дійсно хочете вийти ? ", "Так", "Ні");
        if (answer)
        {
            System.Environment.Exit(0);
        }
    }

    private async void OnTransformToHtmlClicked(object sender, EventArgs e)
    {
        try
        {
            if (_xmlDocument == null)
            {
                return;
            }

            Transform();

            string htmlFilePath = getFilePath("books.html");
            await DisplayAlert("Збережено", $"HTML файл збережено", "OK");
        }
        catch (UnauthorizedAccessException)
        {
            await DisplayAlert("Помилка", "Немає доступу до файлу", "OK");
        }
        catch (Exception ex)
        {
            OutputLabel.Text = $"Помилка при трансформації: {ex.Message}";
        }
    }

    private void Transform()
    {
        try
        {
            XslCompiledTransform xslt = new XslCompiledTransform();

            string xsltPath = getFilePath("template.xslt");

            if (!File.Exists(xsltPath))
            {
                throw new FileNotFoundException($"Не знайдено XSLT-шаблон за шляхом: {xsltPath}");
            }

            try
            {
                xslt.Load(xsltPath);
            }
            catch (XsltException ex)
            {
                OutputLabel.Text = $"Помилка XSLT: {ex.Message} у рядку {ex.LineNumber}, стовпчику {ex.LinePosition}.";
                return;
            }

            string xmlPath = _selectedFilePath;
            string htmlPath = getFilePath("books.html");

            xslt.Transform(xmlPath, htmlPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Помилка під час трансформації: {ex.Message}");
        }
    }

    private static string getFilePath(string fileName)
    {
        string filePath;

        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            filePath = Path.Combine(desktopPath, fileName);
        }
        else
        {
            filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
        }

        return filePath;
    }

    private void OnSortByBkNameClicked(object sender, EventArgs e)
    {
        ShowSubjectPicker();
    }

    private void OnSortByBkInfoClicked(object sender, EventArgs e)
    {
        ShowInfoPicker();
    }

    private void OnSortByAuNameClicked(object sender, EventArgs e)
    {
        ShowAuthorPicker();
    }

    private void ShowAuthorPicker()
    {
        var authors = _xmlDocument.Descendants("author")
                                  .Select(a => a.Attribute("AU_NAME")?.Value)
                                  .Distinct()
                                  .ToList();
        AuthorPicker.ItemsSource = authors;
        AuthorPicker.IsVisible = true;
        SubjectPicker.IsVisible = false;
        InfoPicker.IsVisible = false;
    }

    private void ShowSubjectPicker()
    {
        var subjects = _xmlDocument.Descendants("book")
                                    .Select(b => b.Attribute("DC_NAME")?.Value)
                                    .Distinct()
                                    .ToList();
        SubjectPicker.ItemsSource = subjects;
        SubjectPicker.IsVisible = true;
        AuthorPicker.IsVisible = false;
        InfoPicker.IsVisible = false;
    }

    private void ShowInfoPicker()
    {
        var info = _xmlDocument.Descendants("book")
                               .Select(b => b.Attribute("BK_INFO")?.Value)
                               .Distinct()
                               .ToList();
        InfoPicker.ItemsSource = info;
        InfoPicker.IsVisible = true;
        AuthorPicker.IsVisible = false;
        SubjectPicker.IsVisible = false;
    }

    private void OnAuthorSelected(object sender, EventArgs e)
    {
        string selectedAuthor = AuthorPicker.SelectedItem?.ToString();
        DisplaySelectedData("AU_NAME", selectedAuthor);
    }

    private void OnSubjectSelected(object sender, EventArgs e)
    {
        string selectedSubject = SubjectPicker.SelectedItem?.ToString();
        DisplaySelectedData("DC_NAME", selectedSubject);
    }

    private void OnInfoSelected(object sender, EventArgs e)
    {
        string selectedInfo = InfoPicker.SelectedItem?.ToString();
        DisplaySelectedData("BK_INFO", selectedInfo);
    }

    private void DisplaySelectedData(string filterType, string filterValue)
    {
        if (string.IsNullOrEmpty(filterValue)) return;

        var filteredData = _xmlDocument.Descendants("book")
            .Where(book =>
            {
                if (filterType == "AU_NAME")
                    return book.Descendants("author")
                               .Any(author => author.Attribute("AU_NAME")?.Value == filterValue);

                if (filterType == "DC_NAME")
                    return book.Attribute("DC_NAME")?.Value == filterValue;

                if (filterType == "BK_INFO")
                    return book.Attribute("BK_INFO")?.Value == filterValue;

                return false;
            })
            .Select(book => new
            {
                Name = book.Attribute("BK_NAME")?.Value,
                Info = book.Attribute("BK_INFO")?.Value,
                Subject = book.Attribute("DC_NAME")?.Value,
                Authors = string.Join(", ", book.Descendants("author")
                                                .Select(a => a.Attribute("AU_NAME")?.Value))
            })
            .ToList();

        BooksCollectionView.ItemsSource = filteredData;
    }

    private void OnDomParseClicked(object sender, EventArgs e)
    {
        SetParserStrategy(new DomParser());
        ShowSortingButtons();
    }

    private void OnSaxParseClicked(object sender, EventArgs e)
    {
        SetParserStrategy(new SaxParser());
        ShowSortingButtons();
    }

    private void OnLinqParseClicked(object sender, EventArgs e)
    {
        SetParserStrategy(new LinqToXmlParser());
        ShowSortingButtons();
    }

    private void SetParserStrategy(IXmlParserStrategy parserStrategy)
    {
        _currentParserStrategy = parserStrategy;
    }

    private void ShowSortingButtons()
    {
        SortByBkNameButton.IsVisible = true;
        SortByBkInfoButton.IsVisible = true;
        SortByAuNameButton.IsVisible = true;
    }

    private void ExecuteParsing()
    {
        try
        {
            if (_selectedFilePath != null && _currentParserStrategy != null)
            {
                _currentParserStrategy.Parse(_selectedFilePath, result => OutputLabel.Text = result);
            }
            else
            {
                OutputLabel.Text = "Виберіть XML файл і стратегія парсингу.";
            }
        }
        catch (Exception ex)
        {
            OutputLabel.Text = $"Помилка: {ex.Message}";
        }
    }

    public interface IXmlParserStrategy
    {
        void Parse(string filePath, Action<string> output);
    }
    public class SaxParser : IXmlParserStrategy
    {
        public void Parse(string filePath, Action<string> output)
        {
            using var reader = XmlReader.Create(filePath);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        output($"Start Element: {reader.Name}");
                        break;
                    case XmlNodeType.Text:
                        output($"Text: {reader.Value}");
                        break;
                    case XmlNodeType.EndElement:
                        output($"End Element: {reader.Name}");
                        break;
                }
            }
        }
    }
    public class DomParser : IXmlParserStrategy
    {
        public void Parse(string filePath, Action<string> output)
        {
            var doc = new XmlDocument();
            doc.Load(filePath);
            ProcessElement(doc.DocumentElement, output);
        }
        private void ProcessElement(XmlElement element, Action<string> output)
        {
            if (element == null)
                return;

            output($"Element: {element.Name}");

            foreach (XmlAttribute attribute in element.Attributes)
            {
                output($"Attribute: {attribute.Name} = {attribute.Value}");
            }

            if (!string.IsNullOrEmpty(element.InnerText))
            {
                output($"Text: {element.InnerText}");
            }

            foreach (XmlNode childNode in element.ChildNodes)
            {
                if (childNode is XmlElement childElement)
                {
                    ProcessElement(childElement, output);
                }
            }
        }
    }
    public class LinqToXmlParser : IXmlParserStrategy
    {
        public void Parse(string filePath, Action<string> output)
        {
            var doc = XDocument.Load(filePath);

            foreach (var element in doc.Descendants())
            {
                output($"Element: {element.Name}");

                foreach (var attribute in element.Attributes())
                {
                    output($"Attribute: {attribute.Name} = {attribute.Value}");
                }

                if (!string.IsNullOrEmpty(element.Value))
                {
                    output($"Text: {element.Value}");
                }
            }
        }
    }
}
