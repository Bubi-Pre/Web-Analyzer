using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;

namespace WebAnalyzerPro
{
    public partial class MainForm : Form
    {
        private TextBox urlTextBox;
        private Button analyzeButton;
        private DataGridView resultDataGridView;
        private RichTextBox htmlTextBox;
        private FlowLayoutPanel imagePanel;
        private TextBox searchTextBox;
        private Button searchButton;

        public MainForm()
        {
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
          
        }

        private async void AnalyzeButton_Click(object sender, EventArgs e)
        {
            var url = urlTextBox.Text;
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                var data = await FetchAndAnalyzeData(url);
                DisplayAnalysisResults(data);
            }
            else
            {
                MessageBox.Show("Please enter a valid URL.");
            }
        }

        private void InitializeForm()
        {
            this.ClientSize = new Size(800, 800);
            this.Text = "Web Analyzer Pro";

            urlTextBox = new TextBox { Location = new Point(10, 10), Width = 600, Text = "Enter URL" };
            analyzeButton = new Button { Location = new Point(620, 10), Text = "Analyze" };
            analyzeButton.Click += AnalyzeButton_Click;

            resultDataGridView = new DataGridView
            {
                Location = new Point(10, 40),
                Width = 760,
                Height = 400,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true
            };
            resultDataGridView.Columns.Add("Element", "Element");
            resultDataGridView.Columns.Add("Content", "Content");

   
            resultDataGridView.CellDoubleClick += ResultDataGridView_CellDoubleClick;

            htmlTextBox = new RichTextBox
            {
                Location = new Point(10, 450),
                Width = 760,
                Height = 300,
                WordWrap = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.White,
                ForeColor = Color.Black
            };

            imagePanel = new FlowLayoutPanel { Location = new Point(10, 760), Width = 760, Height = 80, AutoScroll = true };

            searchTextBox = new TextBox { Location = new Point(10, 720), Width = 600, Text = "Search in HTML" };
            searchButton = new Button { Location = new Point(620, 720), Text = "Search" };
            searchButton.Click += SearchButton_Click;

            this.Controls.Add(urlTextBox);
            this.Controls.Add(analyzeButton);
            this.Controls.Add(resultDataGridView);
            this.Controls.Add(htmlTextBox);
            this.Controls.Add(imagePanel);
            this.Controls.Add(searchTextBox);
            this.Controls.Add(searchButton);
        }

        private async Task<SuperAdvancedWebData> FetchAndAnalyzeData(string url)
        {
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            string html;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
                try
                {
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var byteArray = await response.Content.ReadAsByteArrayAsync();
                    var contentType = response.Content.Headers.ContentType;
                    System.Text.Encoding encoding;

                    if (contentType != null && !string.IsNullOrEmpty(contentType.CharSet))
                    {
                        try
                        {
                            encoding = System.Text.Encoding.GetEncoding(contentType.CharSet);
                        }
                        catch (ArgumentException)
                        {
                            encoding = System.Text.Encoding.UTF8;
                        }
                    }
                    else
                    {
                        encoding = System.Text.Encoding.UTF8;
                    }

                    html = encoding.GetString(byteArray);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error fetching data: {ex.Message}");
                    return null;
                }
            }

            htmlDoc.LoadHtml(html);

            var title = htmlDoc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? "No title found";
            var metaTags = htmlDoc.DocumentNode.SelectNodes("//meta")
                ?.Select(node => new MetaData
                {
                    Name = node.GetAttributeValue("name", string.Empty),
                    Content = node.GetAttributeValue("content", string.Empty)
                }).ToList() ?? new List<MetaData>();

            var linkNodes = htmlDoc.DocumentNode.SelectNodes("//a")
                ?.Select(node => new LinkData
                {
                    Text = node.InnerText.Trim(),
                    Url = node.GetAttributeValue("href", string.Empty)
                }).ToList() ?? new List<LinkData>();

            var images = htmlDoc.DocumentNode.SelectNodes("//img[@src]")
                ?.Select(node => node.GetAttributeValue("src", string.Empty)).ToList() ?? new List<string>();

            return new SuperAdvancedWebData
            {
                Title = title,
                MetaTags = metaTags,
                Links = linkNodes,
                Images = images,
                FullHtml = html
            };
        }

        private void DisplayAnalysisResults(SuperAdvancedWebData data)
        {
            resultDataGridView.Rows.Clear();

            if (data == null)
            {
                MessageBox.Show("No data retrieved from the URL. Please check the URL and try again.");
                return;
            }

            resultDataGridView.Rows.Add("Title", data.Title);

            foreach (var meta in data.MetaTags)
            {
                resultDataGridView.Rows.Add($"Meta - {meta.Name}", meta.Content);
            }

            foreach (var link in data.Links)
            {
                resultDataGridView.Rows.Add($"Link - {link.Text}", link.Url);
            }

            imagePanel.Controls.Clear();
            foreach (var imageUrl in data.Images)
            {
                var pictureBox = new PictureBox
                {
                    Size = new Size(75, 75),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Margin = new Padding(5)
                };

                LoadImageAsync(imageUrl, pictureBox);
                imagePanel.Controls.Add(pictureBox);
            }

            htmlTextBox.Text = FormatHtml(data.FullHtml);
        }

        private async void LoadImageAsync(string imageUrl, PictureBox pictureBox)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    using (var stream = new System.IO.MemoryStream(imageBytes))
                    {
                        pictureBox.Image = Image.FromStream(stream);
                    }
                }
            }
            catch
            {
                pictureBox.Image = null;
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            var searchTerm = searchTextBox.Text;
            MessageBox.Show($"Searching for '{searchTerm}' in the HTML content.");
        }

        private string FormatHtml(string html)
        {
            return html;
        }

        private void ResultDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1) 
            {
                var content = resultDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                Clipboard.SetText(content);
                MessageBox.Show("Copied to clipboard: " + content);
            }
        }

        public class SuperAdvancedWebData
        {
            public string Title { get; set; }
            public List<MetaData> MetaTags { get; set; }
            public List<LinkData> Links { get; set; }
            public List<string> Images { get; set; }
            public string FullHtml { get; set; }
        }

        public class MetaData
        {
            public string Name { get; set; }
            public string Content { get; set; }
        }

        public class LinkData
        {
            public string Text { get; set; }
            public string Url { get; set; }
        }
    }
}
