using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestS3Upload
{
    public partial class Form1 : Form
    {
        private readonly string credentialsAddress = "credentialsAddressURL";
        private readonly string bucketName = "bucketURL";
        private static readonly RegionEndpoint region = RegionEndpoint.APNortheast2;
        
        private string pemCertWithPrivateKeyText = string.Empty;
        private AWSCredentials credentials;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string certificatePath = @"crtName.pem.crt";
            string rsaPrivatePath = @"keyName.pem.key";

            if (File.Exists(certificatePath) && File.Exists(rsaPrivatePath))
            {
                string certificateString = File.ReadAllText(certificatePath);
                string rsaPrivateString = File.ReadAllText(rsaPrivatePath);
                pemCertWithPrivateKeyText = certificateString + rsaPrivateString;
            }
            else
            {
                buttonUpload.Enabled = false;
                return;
            }

            X509Certificate2 cert = PEMToX509.Convert(pemCertWithPrivateKeyText);
            var task = Task.Run(() => MakeAWSCredentials(cert));
            task.ContinueWith(x =>
            {
                try
                {
                    credentials = x.Result;
                }
                catch { }
            });
        }

        private async Task<AWSCredentials> MakeAWSCredentials(X509Certificate2 cert)
        {
            AWSCredentials credentials = new AWSCredentials();

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ClientCertificates.Add(cert);
            
            using (var client = new HttpClient(httpClientHandler))
            {
                string jsonaddr = credentialsAddress;
                client.Timeout = new TimeSpan(0, 0, 10);
                try
                {
                    var response = await client.GetAsync(credentialsAddress);
                    if (response.IsSuccessStatusCode)
                    {
                        var str = response.Content.ReadAsStringAsync().Result;
                        PhrsingCredentials temp = JsonConvert.DeserializeObject<PhrsingCredentials>(str);
                        credentials = temp.credentials;
                    }
                    else
                    {
                        string failedMessage = $"Failed MakeAWSCredentials StatusCode:{response.StatusCode} ReasonPhrase:{response.ReasonPhrase}";
                        Console.WriteLine(failedMessage);
                    }
                }
                catch (TaskCanceledException tce)
                {
                    Console.WriteLine(tce.Message);
                    Console.WriteLine(tce.StackTrace);
                }
                catch (HttpRequestException hre)
                {
                    Console.WriteLine(hre.Message);
                    Console.WriteLine(hre.StackTrace);
                }
            }

            return credentials;
        }

        private async Task<bool> S3FileUploadAsync(string filePath)
        {
            bool rslt;
            try
            {
                IAmazonS3 s3Client = new AmazonS3Client(credentials.accessKeyId, credentials.secretAccessKey, credentials.sessionToken, region);
                var transfer = new TransferUtility(s3Client);
                await transfer.UploadAsync(filePath, bucketName);
                rslt = true;
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine(
                        "Error encountered ***. Message:'{0}' when writing an object", e.Message);
                rslt = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
                rslt = false;
            }
            return rslt;
        }

        private void ButtonBrower_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        private bool CredentialsNullAndEmptyCheck()
        {
            bool rslt = true;
            if (credentials == null)
            {
                rslt = false;
            }
            else if (credentials.accessKeyId == null)
            {
                rslt = false;
            }
            else if (credentials.accessKeyId.Length <= 0)
            {
                rslt = false;
            }
            else if (credentials.secretAccessKey == null)
            {
                rslt = false;
            }
            else if (credentials.secretAccessKey.Length <= 0)
            {
                rslt = false;
            }
            else if (credentials.sessionToken == null)
            {
                rslt = false;
            }
            else if (credentials.sessionToken.Length <= 0)
            {
                rslt = false;
            }
            else if (credentials.expiration == null)
            {
                rslt = false;
            }
            else if (credentials.expiration.Length <= 0)
            {
                rslt = false;
            }
            return rslt;
        }

        private void ButtonUpload_Click(object sender, EventArgs e)
        {
            if (!CredentialsNullAndEmptyCheck())
            {
                MessageBox.Show("No Exist Files");
                return;
            }
            
            try
            {
                Console.WriteLine(credentials.expiration);
                DateTime expoirationTime = DateTime.ParseExact(credentials.expiration, "yyyy-MM-ddTHH:mm:ssZ", null);
                Console.WriteLine(expoirationTime);

                TimeSpan timeDiff = DateTime.Now - expoirationTime;
                double diffTotalMiniute = timeDiff.TotalMinutes;
                if(diffTotalMiniute <= 55)
                {
                    X509Certificate2 cert = PEMToX509.Convert(pemCertWithPrivateKeyText);
                    var task = Task.Run(() => MakeAWSCredentials(cert));
                    task.ContinueWith(x =>
                    {
                        try
                        {
                            credentials = x.Result;
                        }
                        catch { }
                    });
                }

            }
            catch { }

            string filePath = textBox1.Text;
            if (File.Exists(filePath))
            {
                var task = Task.Run(() => S3FileUploadAsync(filePath));
                task.ContinueWith(y =>
                {
                    if (y.Result)
                    {
                        MessageBox.Show("Success: File Upload");
                    }
                    else
                    {
                        MessageBox.Show("Failed: File Upload");
                    }
                });
            }
            else
            {
                MessageBox.Show("No Exist Files orPath");
            }
        }
    }
}
