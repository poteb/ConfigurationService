using pote.Config.Middleware.Secrets;

namespace pote.Config.Middleware.TestApi;

public partial class MySecrets
{
    [Secret]
    private string _secret1 = "";

    //  public string Secret1
    //  {
    //      get
    //      {
    //          if (!_isSecret1Set)
    //              _secret1 = SecretResolver.ResolveSecret(_secret1).Result;
    //          _isSecret1Set = true;
    //          return _secret1;
    //      }
    //      set => _secret1 = value;
    //  }
    //
    // public ISecretResolver SecretResolver { get; set; } = null!;
}

public partial class MySecrets2
{
    private string _secret1 = "";
    private bool _isSecret1Set = false;

     public string Secret1
     {
         get
         {
             if (!_isSecret1Set)
                 _secret1 = SecretResolver.ResolveSecret(_secret1);
             _isSecret1Set = true;
             return _secret1;
         }
         set => _secret1 = value;
     }
    
    public ISecretResolver SecretResolver { get; set; } = null!;
}