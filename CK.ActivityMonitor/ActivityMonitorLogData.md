# ActivityMonitorLogData details

This struct holds the data common to any log event (except the closing group that is not a concern of this library): text, binary, level,
tags, logTime, exception, file name and line number.

Currently, it is a mutable big struct always passed by reference: it always lies on the stack except in the corner
case of the (ActivityMonitor.InternalMonitor)[Impl/ActivityMonitor.InternalMonitor.cs] that handles buggy clients.

## Goal: Zero allocation logging 

Groups (see (Impl/ActimvityMonitor.Groups.cs)[Impl/ActimvityMonitor.Groups.cs]}) are pooled by each monitor and reused: their life
cycle is directly driven by the Open/CloseGroup of the monitor API. When groups 

This struct passed by reference works great in terms of allocations as long as the data doesn't need to be queued for a deferred
handling ant that's exactly what the GrandOutput (in CK.Monitoring) does.

To achieve a true zero allocation implementation, this needs to be changed: ActivityMonitorLogData would better be a reference
type that is pooled and whose release can be deferred (a IDisposable with a reference counter would be fine).

CK.Monitoring already implements a LogEntry model based on a set of immutable internal classes and public "rich" interfaces that
captures, once for all, log data.

However, these are immutables and not disposable and this shouldn't be changed: they capture final logged data and making them disposable
would be a nightmare.

To emit logs we could reuse the interfaces (they should then be defined in CK.ActivityMonitor) and introduce a IDisposable level that
handles the reference counter and the pooling.

## Goal: Utf8 capture and binary content

Currently the only content is the `Text` that is a string (and a string requires an allocation).
Logging JSON payload is rather inefficient: the JSON has to be encoded in UTF-16 in the Text string to
eventually be written/serialized in UTF-8 encoding.

The efficient Utf8JsonWriter directly writes to a IBufferWriter that can be a (RecyclableMemoryStream)[https://github.com/microsoft/Microsoft.IO.RecyclableMemoryStream]
(that uses a pool of reusable buffers).
The idea is to support binary payloads thanks to RecyclableMemoryStream for:
- computing once on demand the UTF-8 Text string representation and cache it when Text is the logged data.
- offers new ways to log different type of content:
```c#
// Using a Utf8Writer.
// here using the new C# 10 u8 suffix: https://github.com/dotnet/csharplang/blob/main/proposals/utf8-string-literals.md.
monitor.Info( w => { w.WriteStartObject(); w.WriteNumber("Power"u8, _power ); w.WriteEndObject()} );

// Using a binary writer (binary serialization):
monitor.Info( w => user.Write( w ) );
```
File content may also be logged:
```c#
// Using a Stream (Helper.CopyFile doesn't exist).
monitor.Info( s => Helper.CopyFile( "appsetings.json", s ) );

// May be better to offer easier extension methods... 
monitor.InfoFile( "appsetings.json" );
```

Such binary content goes into the internal RecyclableMemeoryStream of the ActivityMonitorLogData.

This requires to introduce a notion of "content type" that doesn't exist yet.
We may create a new enum (Text, JSON, Binary...) but this sounds limited. We should simply use Tags to
decorate/qualify the content type and define:
- "BIN" for binary content.
- "UTF8" for Utf8 string.
- "JSON" for Utf8 encoded Json. Technically this is [JSON with Comments](https://code.visualstudio.com/docs/languages/json#_json-with-comments)

The `Text` property, if needed, can always be derived from the binary content:
- for "UTF8", "JSON" by encoding the UTF-8 content back to UTF-16.
- for binary, a simple `<binary data>` string can be used.

## More optimizations

Interpolated string handlers write their content to a (generally) cached and reusable StringBuilder. The StringBuilder now exposes its
`ReadOnlyMemory<char>` chunks of buffers: this can be used to skip the final string generation and directly encode the interpolation
result as an "UTF8" string into the RecyclableMemoryStream.

Providing that the StringBuilder is a reused one and the `Text` property is NOT solicited later on (the client and handlers only need the Utf8 content),
this would lead to a real zero allocation logging.

## Impacts

This has a lot of impacts but totally source based (risks are low) but more importantly requires time that we don't currently have: all this
can be postponed after the current work on the log centralization.




