using System.Text;
using System.Text.Json;

class ZapierWebhook
{
    public required string WebhookUrl { get; set; }

    // this class should take a generic object and send it to a webhook URL
    public async Task<bool> Send(object data)
    {
        using (var client = new HttpClient())
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(this.WebhookUrl, content);
            
            return response.IsSuccessStatusCode;
        }
    }
}