# Mal.MdkModMixin.Network

> [!NOTE]
> **Version:** 1.0.0  
> **Authors:** Malware  
> **Package:** `Mal.MdkModMixin.Network`
> 
> **Description:** Networking for Space Engineers mods: one channel per mod carrying any number of message types, with sends to the server, to one player, to everyone or to players nearby.
>
> **[Installation Instructions →](./Mal.MdkModMixin.Network-Installation.md)** | **[Release Notes →](./Mal.MdkModMixin.Network-ReleaseNotes.md)**

---
# Networking for Space Engineers mods

One network channel per mod, carrying any number of message types. Messages are plain structs you write and read
yourself; each message type is registered on the channel with three functions: how many bytes it takes, how to
write it, and how to read it back. Sends go to the server, to everyone, to the players near a point, or, from the
server, to one player.

## Quickstart

1. **Install the NuGet package** `Mal.MdkModMixin.Network` in your mod project.
2. **Describe each message** as a struct with static `SizeOf`, `Write` and `Read` functions.
3. **Build one channel** for your mod, registering every message type it carries.
4. **Listen and send.**

```csharp
public struct Greeting
{
    public string Text;

    public static int SizeOf(ref Greeting msg)
    {
        return ByteData.SizeOfString(msg.Text);
    }

    public static int Write(ref Greeting msg, byte[] data, ref int offset)
    {
        var start = offset;
        ByteData.WriteString(msg.Text, data, ref offset);
        return offset - start;
    }

    public static int Read(byte[] data, ref int offset, out Greeting msg)
    {
        var start = offset;
        msg = new Greeting();
        ByteData.TryReadString(data, ref offset, out msg.Text);
        return offset - start;
    }
}

NetworkChannel _channel;

public override void BeforeStart()
{
    _channel = NetworkChannel.Create(GetType())
        .UsingModel<Greeting>(Greeting.SizeOf, Greeting.Read, Greeting.Write)
        .Build();
    _channel.ListenFor<Greeting>(OnGreeting);
}

void OnGreeting(ushort channelId, Greeting msg, ulong senderId, bool fromServer)
{
    MyLog.Default.WriteLine(senderId + " says " + msg.Text);
}

protected override void UnloadData()
{
    _channel?.Dispose();
    _channel = null;
}
```

Use one channel per mod: a channel carries every message type the mod registers on it.

## Channel ids

`NetworkChannel.Create` takes a type (its full name is used), a name, or a 16-bit id. Pick something only your mod
uses, such as your session component's type or a name that includes your mod's name.

## Sending

| Call | Goes to |
|---|---|
| `SendToServer(msg, reliable)` | The server. |
| `Send(msg, steamId, reliable)` | From the server, one player. From a client, only its own machine, by way of the server and marked as from the server; the game drops a client's message to another player, so ask the server to send it. |
| `Broadcast(msg, reliable, includingSelf)` | Every other player, and this machine too when `includingSelf` is true. |
| `SendToNearby(msg, origin, radius, reliable, includingSelf)` | Every player within `radius` metres of `origin` who has a character (bots and players not spawned are skipped); the sender too only when `includingSelf` is true. From a client, the message goes by way of the server. A negative or NaN radius, or an origin that is NaN or infinite, reaches no one. |

## Receiving

`ListenFor<T>(handler)` calls the handler with each message of that type: the channel id, the message, the sender's
Steam id, and whether it came from the server. `StopListeningFor<T>` stops it.

## Writing and reading messages

`ByteData` writes and reads integers, floats, doubles, vectors and strings, little-endian.

## Testing

`NetworkChannel.Network` is the channel's way to the game's multiplayer, an `INetworkProxy`. Set it to a stand-in
to test sending and receiving outside the game.

---

*Documentation auto-generated from package metadata. Last updated: 2026-10-03*
