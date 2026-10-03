using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Checkpoint.Core;
namespace Checkpoint.App;
internal sealed class IgdbClient : IDisposable
{
    private readonly HttpClient http;
    private readonly Uri endpoint;
    internal IgdbClient(SocialProject project,HttpMessageHandler? handler=null)
    {
        endpoint=new Uri(project.Validate(),"functions/v1/checkpoint-covers/");
        http=new(handler??new HttpClientHandler { AllowAutoRedirect=false }) { Timeout=TimeSpan.FromSeconds(35),MaxResponseContentBufferSize=2_000_000 };
        http.DefaultRequestHeaders.Add("apikey",project.PublishableKey);
    }
    internal async Task<IgdbCover?> Search(string title,System.Collections.Generic.List<string> excluded,CancellationToken cancellation=default,bool refresh=false)
    {
        if(string.IsNullOrWhiteSpace(title)||title.Trim().Length>140)throw new ArgumentException(I18n.T("El nombre debe tener entre 1 y 140 caracteres."));
        using var response=await http.PostAsJsonAsync(new Uri(endpoint,"v1/search"),new {title=title.Trim(),excluded,refresh},DataJson.Options,cancellation);
        if(!response.IsSuccessStatusCode)throw new InvalidOperationException(await Error(response));
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
        var value=json.RootElement.GetProperty("candidate");if(value.ValueKind==JsonValueKind.Null)return null;
        var candidate=value.Deserialize<IgdbCover>(DataJson.Options);
        if(candidate is null||candidate.Id<=0||string.IsNullOrWhiteSpace(candidate.Name)||candidate.Name.Length>250||!CoverSuggestions.ValidImageId(candidate.ImageId)||excluded.Contains(candidate.ImageId)||!double.IsFinite(candidate.Score)||candidate.Score is <0 or >1)
            throw new InvalidOperationException(I18n.T("IGDB no está disponible. Inténtalo más tarde."));
        return candidate;
    }
    internal async Task<byte[]> Image(string id,CancellationToken cancellation=default)
    {
        if(!CoverSuggestions.ValidImageId(id))throw new ArgumentException(I18n.T("La configuración de carátulas no es válida."));
        using var response=await http.GetAsync(new Uri(endpoint,"v1/image/"+id),cancellation);
        if(!response.IsSuccessStatusCode||response.Content.Headers.ContentType?.MediaType?.StartsWith("image/",StringComparison.Ordinal)!=true)
            throw new InvalidOperationException(I18n.T("IGDB no está disponible. Inténtalo más tarde."));
        return await response.Content.ReadAsByteArrayAsync(cancellation);
    }
    private static async Task<string> Error(HttpResponseMessage response)
    {
        string? code=null;try{using var value=JsonDocument.Parse(await response.Content.ReadAsStringAsync());code=value.RootElement.GetProperty("code").GetString();}catch(JsonException){}
        return code switch {
            "igdb-not-configured"=>I18n.T("El responsable de esta edición debe configurar IGDB en Supabase."),
            "rate"=>I18n.T("Demasiadas consultas. Espera antes de volver a intentarlo."),
            _=>I18n.T("IGDB no está disponible. Inténtalo más tarde.")};
    }
    public void Dispose()=>http.Dispose();
}
