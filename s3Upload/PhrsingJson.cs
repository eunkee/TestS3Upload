namespace TestS3Upload
{
    public class PhrsingCredentials
    {
        public AWSCredentials credentials { get; set; }
    }

    public class AWSCredentials
    {
        public string accessKeyId { get; set; }
        public string secretAccessKey { get; set; }
        public string sessionToken { get; set; }
        public string expiration { get; set; }
    }
}
