using System;
using System.Data;
using System.Data.Oleb;

class APICollection
{
	static void Main ()
	{
		String connectionString = "DataSource = (MySql_connection);
	}
		AtringQuery = "Select ProductId"

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
    
}
