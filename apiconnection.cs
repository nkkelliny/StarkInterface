using System;
using System.Data;
using System.Data.OleDb;

class APICollection
{
	static void Main ()
	{

	HTTPWebRequest request = (HttpWebREquest)WebRequest.Create("http://a/uri");
	request.Method = "Post";
	request.Headers.Add("Authorization: OAuth" +accessToken);
	string postData = string.Format("param1=something&param2=something_else");
	byte[] data = Encoding.UTF8.GetBytes(postData);

	request.ContentType = "application/x-www-form-urlencoded";
	request.Accept = "application/json";
	request.ContentLength = data.Length;

	using (Stream requestStream = request.GetRequestStream())
	{
    	requestStream.Write(data, 0, data.Length);
	}

	try
	{
    	using(WebResponse response = request.GetResponse())
    	{
        
   	 }
	}
	catch (WebException ex)
	{
		
	System.Windows.Forms.RichTextBox rtBox = new System.Windows.Forms.RichTextBox();
	string rtfText = System.IO.File.ReadAllText(path);
	System.Windows.Forms.MessageBox.Show(rtfText);
	rtBox.Rtf = rtText;
	string plainText = rtBox.Text;
	System.Windows.Forms.MessageBox.Show(plainText);
	System.IO.File.WriteAllTExt(@"output.txt", plainText);
	}
    
}
