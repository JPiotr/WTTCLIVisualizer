using System;
using System.Text.Json;
using WTTCLIVizualizer.Data;

namespace WTTCLIVizualizer.Logic;

public class DataGetter
{
    public static async Task<Root?> ReadAndDeserializeJsonAsync(string filePath)
    {
        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            var options = new JsonSerializerOptions(){
                Converters = {new CustomDateTimeConverter()}
            };
            return await JsonSerializer.DeserializeAsync<Root>(fileStream,options); 
        }
    }
}
