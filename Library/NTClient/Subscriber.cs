using System.Text.Json;
using System.Text.Json.Nodes;

namespace NTClient
{
  public class Subscriber
  {
    private string[] topics;
    private int uid;
    private SubscriptionOptions options = new SubscriptionOptions();

    public Subscriber(string[] topics, int uid, SubscriptionOptions options)
    {
      this.topics = topics;
      this.uid = uid;
      this.options = options;
    }

    public Subscriber(string topic, int uid, SubscriptionOptions options)
    {
      topics = [topic];
      this.uid = uid;
      this.options = options;
    }

    public string[] Topics
    {
      get { return topics; }
      set { topics = value; }
    }

    public int Uid
    {
      get { return uid; }
      set { uid = value; }
    }

    public SubscriptionOptions Options
    {
      get { return options; }
      set { options = value; }
    }

    public Dictionary<string, object> ForSubscribe()
    {
      var subscribe = new Dictionary<string, object>
      {
        { "topics", topics},
        { "subuid", uid },
        { "options", options.ToOptions() }
      };
      Console.WriteLine(subscribe["topics"]);
      return subscribe;

;
    }

    public Dictionary<string, object> ForUnsubscribe()
    {
      var unubscribe = new Dictionary<string, object>
      {
        { "subuid", uid },
      };
      return unubscribe;
    }
  }

  public class SubscriptionOptions
  {
    private double periodic = 0.1;
    private bool all = false;
    private bool topicsOnly = false;
    private bool prefix = false;



    public Dictionary<string, object> ToOptions()
    {
      var options = new Dictionary<string, object>
      {
        { "periodic", periodic },
        { "all", all },
        { "topicsonly", topicsOnly },
        { "prefix", prefix }
      };

      return options;
    }
  }
}
