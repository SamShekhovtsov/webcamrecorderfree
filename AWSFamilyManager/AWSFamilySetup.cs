using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AWSFamilyManager
{
  public class AWSFamilySetup
  {
    private AmazonRekognitionClient rekognitionClient = new AmazonRekognitionClient();

    private static readonly string familyCollectionId = "closest-family-collection";

    private async Task CreateMyClosestFamilyCollection() {

      var createCollectionRequest = new CreateCollectionRequest
      {
        CollectionId = familyCollectionId
      };
      var createResponse = await rekognitionClient.CreateCollectionAsync(createCollectionRequest);
    }

    private async Task AddFamilyMemberFaces(MemoryStream familyMemberFacePhoto, string familyMemberName = "dad_01") {
      var indexFacesRequest = new IndexFacesRequest
      {
        CollectionId = familyCollectionId,
        Image = new Image { Bytes = familyMemberFacePhoto },
        ExternalImageId = familyMemberName, // Optional: Store a simple ID with the face
        MaxFaces = 1,
        DetectionAttributes = new List<string> { "ALL" }
      };

      var indexResponse = await rekognitionClient.IndexFacesAsync(indexFacesRequest);
      foreach (var record in indexResponse.FaceRecords)
      {
        Console.WriteLine($"Face Indexed: {record.Face.FaceId}");
        // SAVE record.Face.FaceId and "Dad" to your database here.
      }
    }

    private async Task AddMemberFace(string memberId, string faceImagePath) {
      string filePath = faceImagePath;

      // Read all bytes from the file
      byte[] imageBytes = File.ReadAllBytes(filePath);

      // Create a MemoryStream from the byte array
      using MemoryStream dad01Ms = new MemoryStream(imageBytes);

      // The stream is now ready to use
      // For example, creating a System.Drawing.Image from it:
      // var image = System.Drawing.Image.FromStream(ms);
      await AddFamilyMemberFaces(dad01Ms, memberId);
    }

    public async Task SetupFamily() {
      try
      {
        await CreateMyClosestFamilyCollection();

        for (int i = 1; i <= 7; i++)
        {
          await AddMemberFace($@"dad_{i.ToString("D2")}", $@"D:\\Temp\\PhotosTemp\\dad_{i.ToString("D2")}.jpg");
        }

        for (int i = 1; i <= 7; i++)
        {
          await AddMemberFace($@"daugher_{i.ToString("D2")}", $@"D:\\Temp\\PhotosTemp\\daugher_{i.ToString("D2")}.jpg");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        throw;
      }
    }
  }
}
