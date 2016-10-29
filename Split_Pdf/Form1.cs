using System;
using System.IO;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Path = System.IO.Path;

namespace Split_Pdf
{
    public partial class Form1 : Form
    {
        public Form1(){
            InitializeComponent();
        }

        private void btnChooseFile_Click(object sender, EventArgs e){
            var ofd = new OpenFileDialog();
            ofd.ShowDialog();
            selectedFile.Text = ofd.FileName;
        }

        private void btnSaveLocation_Click(object sender, EventArgs e)
        {
            var ofd = new FolderBrowserDialog();
            ofd.ShowDialog();
            txtSaveLocation.Text = ofd.SelectedPath;
            if ((!string.IsNullOrWhiteSpace(selectedFile.Text)) && (!string.IsNullOrWhiteSpace(txtSaveLocation.Text)))
                btnStart.Enabled = true;
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            StartSplit();
        }

        private void StartSplit()
        {
			try
			{
				var extension = Path.GetExtension(selectedFile.Text);
				if (extension != null && extension.ToLower() != ".pdf"){
					MessageBox.Show("INVALID DOCUMENT FORMAT " + Environment.NewLine + "PLEASE CHOOSE PDF FORMAT ONLY", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				if (string.IsNullOrWhiteSpace(txtSaveLocation.Text)){
					MessageBox.Show("Please choose save location");
					return;
				}

				var frontFolderName = "Part_1";
				var backFolderName = "Part_2";

				if(!Directory.Exists(txtSaveLocation.Text+@"/"+frontFolderName))
					Directory.CreateDirectory(txtSaveLocation.Text+@"/"+frontFolderName);
				
				if(!Directory.Exists(txtSaveLocation.Text+@"/"+backFolderName))
					Directory.CreateDirectory(txtSaveLocation.Text+@"/"+backFolderName);
				
				using (PdfReader reader = new PdfReader(selectedFile.Text))
				{
					PdfReader.unethicalreading = true;
					var frontFiles = new string[0];
					var backFiles = new string[0];
					totalPage.Text = reader.NumberOfPages.ToString();
					var start = string.IsNullOrWhiteSpace(skip.Text) ? 1 : int.Parse(skip.Text);

					for (var i = start; i <= reader.NumberOfPages; i++)
					{
						var isFront = ((i % 2) != 0);
						var fileName = "";
						if(isFront)
							fileName = txtSaveLocation.Text+@"/"+frontFolderName+@"/" + i + ".pdf";
						else
							fileName = txtSaveLocation.Text+@"/"+backFolderName+@"/" + i + ".pdf";
						
						 //fileName = txtSaveLocation.Text + @"/" + isFront + "_" + i + ".pdf";
						ExtractPages(reader, fileName, i);

						if (isFront){
							Array.Resize(ref frontFiles, frontFiles.Length + 1);
							frontFiles[frontFiles.Length - 1] = fileName;
						}
						else{
							Array.Resize(ref backFiles, backFiles.Length + 1);
							backFiles[backFiles.Length - 1] = fileName;
						}
					}
					CombineMultiplePDFs(frontFiles,txtSaveLocation.Text,true);
					Array.Reverse (backFiles);
					CombineMultiplePDFs(backFiles, txtSaveLocation.Text, false);
					MessageBox.Show("Operation Completed");
				}
			}
			catch(Exception ex) {
				MessageBox.Show(ex.Message);
			}   
        }

        private static void ExtractPages(PdfReader inputPdf, string outputFile, int pageNo)
        {
			try
			{
				// load the input document
				var inputDoc = new Document(inputPdf.GetPageSizeWithRotation(1));

				// create the filestream
				using (var fs = new FileStream(outputFile, FileMode.Create))
				{
					// create the output writer 
					PdfWriter outputWriter = PdfWriter.GetInstance(inputDoc, fs);
					inputDoc.Open();
					PdfContentByte cb1 = outputWriter.DirectContent;

					// copy pages from input to output document

					inputDoc.SetPageSize(inputPdf.GetPageSizeWithRotation(pageNo));
					inputDoc.NewPage();

					PdfImportedPage page = outputWriter.GetImportedPage(inputPdf, pageNo);
					int rotation = inputPdf.GetPageRotation(pageNo);

					if (rotation == 90 || rotation == 270)
					{
						cb1.AddTemplate(page, 0, -1f, 1f, 0, 0,
							inputPdf.GetPageSizeWithRotation(pageNo).Height);
					}
					else
					{
						cb1.AddTemplate(page, 1f, 0, 0, 1f, 0, 0);
					}
					inputDoc.Close();
				}	
			}
			catch(Exception ex){
				MessageBox.Show(ex.Message);
			}          
        }

        private void CombineMultiplePDFs(string[] files, string path, bool isFront)
        {
            var part1FileName = path + @"/part_1.pdf";
            var part2FileName = path + @"/part_2.pdf";
            var outFile = (isFront ? part1FileName : part2FileName);

            // step 1: creation of a document-object
            Document document = new Document();
            document.NewPage();
            // step 2: we create a writer that listens to the document
            PdfCopy writer = new PdfCopy(document, new FileStream(outFile, FileMode.Create));
            if (writer == null)
                return;

            // step 3: we open the document
            document.Open();
         
            foreach (string fileName in files)
            {
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    // we create a reader for a certain document
                    var reader = new PdfReader(fileName);
                    reader.ConsolidateNamedDestinations();

                    // step 4: we add content
                    for (int i = 1; i <= reader.NumberOfPages; i++){
                        PdfImportedPage page = writer.GetImportedPage(reader, i);
                        writer.AddPage(page);
                    }
                    reader.Close();
                }
            }

            writer.Close();
            document.Close();
        }
    }
}