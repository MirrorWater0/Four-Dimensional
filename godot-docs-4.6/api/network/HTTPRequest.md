# HTTPRequest

## Meta

- Name: HTTPRequest
- Source: HTTPRequest.xml
- Inherits: Node
- Inheritance Chain: HTTPRequest -> Node -> Object

## Brief Description

A node with the ability to send HTTP(S) requests.

## Description

A node with the ability to send HTTP requests. Uses HTTPClient internally. Can be used to make HTTP requests, i.e. download or upload files or web content via HTTP. **Warning:** See the notes and warnings on HTTPClient for limitations, especially regarding TLS security. **Note:** When exporting to Android, make sure to enable the INTERNET permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android. **Example:** Contact a REST API and print one of its returned fields:

```
func _ready():
    # Create an HTTP request node and connect its completion signal.
    var http_request = HTTPRequest.new()
    add_child(http_request)
    http_request.request_completed.connect(self._http_request_completed)

    # Perform a GET request. The URL below returns JSON as of writing.
    var error = http_request.request("https://httpbin.org/get")
    if error != OK:
        push_error("An error occurred in the HTTP request.")

    # Perform a POST request. The URL below returns JSON as of writing.
    # Note: Don't make simultaneous requests using a single HTTPRequest node.
    # The snippet below is provided for reference only.
    var body = JSON.new().stringify({"name": "Godette"})
    error = http_request.request("https://httpbin.org/post", [], HTTPClient.METHOD_POST, body)
    if error != OK:
        push_error("An error occurred in the HTTP request.")

# Called when the HTTP request is completed.
func _http_request_completed(result, response_code, headers, body):
    var json = JSON.new()
    json.parse(body.get_string_from_utf8())
    var response = json.get_data()

    # Will print the user agent string used by the HTTPRequest node (as recognized by httpbin.org).
    print(response.headers["User-Agent"])
```

```
public override void _Ready()
{
    // Create an HTTP request node and connect its completion signal.
    var httpRequest = new HttpRequest();
    AddChild(httpRequest);
    httpRequest.RequestCompleted += HttpRequestCompleted;

    // Perform a GET request. The URL below returns JSON as of writing.
    Error error = httpRequest.Request("https://httpbin.org/get");
    if (error != Error.Ok)
    {
        GD.PushError("An error occurred in the HTTP request.");
    }

    // Perform a POST request. The URL below returns JSON as of writing.
    // Note: Don't make simultaneous requests using a single HTTPRequest node.
    // The snippet below is provided for reference only.
    string body = new Json().Stringify(new Godot.Collections.Dictionary
    {
        { "name", "Godette" }
    });
    error = httpRequest.Request("https://httpbin.org/post", null, HttpClient.Method.Post, body);
    if (error != Error.Ok)
    {
        GD.PushError("An error occurred in the HTTP request.");
    }
}

// Called when the HTTP request is completed.
private void HttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
{
    var json = new Json();
    json.Parse(body.GetStringFromUtf8());
    var response = json.GetData().AsGodotDictionary();

    // Will print the user agent string used by the HTTPRequest node (as recognized by httpbin.org).
    GD.Print((response["headers"].AsGodotDictionary())["User-Agent"]);
}
```

**Example:** Load an image using HTTPRequest and display it:

```
func _ready():
    # Create an HTTP request node and connect its completion signal.
    var http_request = HTTPRequest.new()
    add_child(http_request)
    http_request.request_completed.connect(self._http_request_completed)

    # Perform the HTTP request. The URL below returns a PNG image as of writing.
    var error = http_request.request("https://placehold.co/512.png")
    if error != OK:
        push_error("An error occurred in the HTTP request.")

# Called when the HTTP request is completed.
func _http_request_completed(result, response_code, headers, body):
    if result != HTTPRequest.RESULT_SUCCESS:
        push_error("Image couldn't be downloaded. Try a different image.")

    var image = Image.new()
    var error = image.load_png_from_buffer(body)
    if error != OK:
        push_error("Couldn't load the image.")

    var texture = ImageTexture.create_from_image(image)

    # Display the image in a TextureRect node.
    var texture_rect = TextureRect.new()
    add_child(texture_rect)
    texture_rect.texture = texture
```

```
public override void _Ready()
{
    // Create an HTTP request node and connect its completion signal.
    var httpRequest = new HttpRequest();
    AddChild(httpRequest);
    httpRequest.RequestCompleted += HttpRequestCompleted;

    // Perform the HTTP request. The URL below returns a PNG image as of writing.
    Error error = httpRequest.Request("https://placehold.co/512.png");
    if (error != Error.Ok)
    {
        GD.PushError("An error occurred in the HTTP request.");
    }
}

// Called when the HTTP request is completed.
private void HttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
{
    if (result != (long)HttpRequest.Result.Success)
    {
        GD.PushError("Image couldn't be downloaded. Try a different image.");
    }
    var image = new Image();
    Error error = image.LoadPngFromBuffer(body);
    if (error != Error.Ok)
    {
        GD.PushError("Couldn't load the image.");
    }

    var texture = ImageTexture.CreateFromImage(image);

    // Display the image in a TextureRect node.
    var textureRect = new TextureRect();
    AddChild(textureRect);
    textureRect.Texture = texture;
}
```

**Note:** HTTPRequest nodes will automatically handle decompression of response bodies. An Accept-Encoding header will be automatically added to each of your requests, unless one is already specified. Any response with a Content-Encoding: gzip header will automatically be decompressed and delivered to you as uncompressed bytes.

## Quick Reference

```
[methods]
cancel_request() -> void
get_body_size() -> int [const]
get_downloaded_bytes() -> int [const]
get_http_client_status() -> int (HTTPClient.Status) [const]
request(url: String, custom_headers: PackedStringArray = PackedStringArray(), method: int (HTTPClient.Method) = 0, request_data: String = "") -> int (Error)
request_raw(url: String, custom_headers: PackedStringArray = PackedStringArray(), method: int (HTTPClient.Method) = 0, request_data_raw: PackedByteArray = PackedByteArray()) -> int (Error)
set_http_proxy(host: String, port: int) -> void
set_https_proxy(host: String, port: int) -> void
set_tls_options(client_options: TLSOptions) -> void

[properties]
accept_gzip: bool = true
body_size_limit: int = -1
download_chunk_size: int = 65536
download_file: String = ""
max_redirects: int = 8
timeout: float = 0.0
use_threads: bool = false
```

## Tutorials

- [Making HTTP requests]($DOCS_URL/tutorials/networking/http_request_class.html)
- [TLS certificates]($DOCS_URL/tutorials/networking/ssl_certificates.html)

## Methods

- cancel_request() -> void
  Cancels the current request.

- get_body_size() -> int [const]
  Returns the response body length. **Note:** Some Web servers may not send a body length. In this case, the value returned will be -1. If using chunked transfer encoding, the body length will also be -1.

- get_downloaded_bytes() -> int [const]
  Returns the number of bytes this HTTPRequest downloaded.

- get_http_client_status() -> int (HTTPClient.Status) [const]
  Returns the current status of the underlying HTTPClient.

- request(url: String, custom_headers: PackedStringArray = PackedStringArray(), method: int (HTTPClient.Method) = 0, request_data: String = "") -> int (Error)
  Creates request on the underlying HTTPClient. If there is no configuration errors, it tries to connect using HTTPClient.connect_to_host() and passes parameters onto HTTPClient.request(). Returns OK if request is successfully created. (Does not imply that the server has responded), ERR_UNCONFIGURED if not in the tree, ERR_BUSY if still processing previous request, ERR_INVALID_PARAMETER if given string is not a valid URL format, or ERR_CANT_CONNECT if not using thread and the HTTPClient cannot connect to host. **Note:** When method is HTTPClient.METHOD_GET, the payload sent via request_data might be ignored by the server or even cause the server to reject the request (check [RFC 7231 section 4.3.1](https://datatracker.ietf.org/doc/html/rfc7231#section-4.3.1) for more details). As a workaround, you can send data as a query string in the URL (see String.uri_encode() for an example). **Note:** It's recommended to use transport encryption (TLS) and to avoid sending sensitive information (such as login credentials) in HTTP GET URL parameters. Consider using HTTP POST requests or HTTP headers for such information instead.

- request_raw(url: String, custom_headers: PackedStringArray = PackedStringArray(), method: int (HTTPClient.Method) = 0, request_data_raw: PackedByteArray = PackedByteArray()) -> int (Error)
  Creates request on the underlying HTTPClient using a raw array of bytes for the request body. If there is no configuration errors, it tries to connect using HTTPClient.connect_to_host() and passes parameters onto HTTPClient.request(). Returns OK if request is successfully created. (Does not imply that the server has responded), ERR_UNCONFIGURED if not in the tree, ERR_BUSY if still processing previous request, ERR_INVALID_PARAMETER if given string is not a valid URL format, or ERR_CANT_CONNECT if not using thread and the HTTPClient cannot connect to host.

- set_http_proxy(host: String, port: int) -> void
  Sets the proxy server for HTTP requests. The proxy server is unset if host is empty or port is -1.

- set_https_proxy(host: String, port: int) -> void
  Sets the proxy server for HTTPS requests. The proxy server is unset if host is empty or port is -1.

- set_tls_options(client_options: TLSOptions) -> void
  Sets the TLSOptions to be used when connecting to an HTTPS server. See TLSOptions.client().

## Properties

- accept_gzip: bool = true [set set_accept_gzip; get is_accepting_gzip]
  If true, this header will be added to each request: Accept-Encoding: gzip, deflate telling servers that it's okay to compress response bodies. Any Response body declaring a Content-Encoding of either gzip or deflate will then be automatically decompressed, and the uncompressed bytes will be delivered via request_completed. If the user has specified their own Accept-Encoding header, then no header will be added regardless of accept_gzip. If false no header will be added, and no decompression will be performed on response bodies. The raw bytes of the response body will be returned via request_completed.

- body_size_limit: int = -1 [set set_body_size_limit; get get_body_size_limit]
  Maximum allowed size for response bodies. If the response body is compressed, this will be used as the maximum allowed size for the decompressed body.

- download_chunk_size: int = 65536 [set set_download_chunk_size; get get_download_chunk_size]
  The size of the buffer used and maximum bytes to read per iteration. See HTTPClient.read_chunk_size. Set this to a lower value (e.g. 4096 for 4 KiB) when downloading small files to decrease memory usage at the cost of download speeds.

- download_file: String = "" [set set_download_file; get get_download_file]
  The file to download into. Will output any received file into it.

- max_redirects: int = 8 [set set_max_redirects; get get_max_redirects]
  Maximum number of allowed redirects.

- timeout: float = 0.0 [set set_timeout; get get_timeout]
  The duration to wait before a request times out, in seconds (independent of Engine.time_scale). If timeout is set to 0.0, the request will never time out. For simple requests, such as communication with a REST API, it is recommended to set timeout to a value suitable for the server response time (commonly between 1.0 and 10.0). This will help prevent unwanted timeouts caused by variation in response times while still allowing the application to detect when a request has timed out. For larger requests such as file downloads, it is recommended to set timeout to 0.0, disabling the timeout functionality. This will help prevent large transfers from failing due to exceeding the timeout value.

- use_threads: bool = false [set set_use_threads; get is_using_threads]
  If true, multithreading is used to improve performance.

## Signals

- request_completed(result: int, response_code: int, headers: PackedStringArray, body: PackedByteArray)
  Emitted when a request is completed.

## Constants

### Enum Result

- RESULT_SUCCESS = 0
  Request successful.

- RESULT_CHUNKED_BODY_SIZE_MISMATCH = 1
  Request failed due to a mismatch between the expected and actual chunked body size during transfer. Possible causes include network errors, server misconfiguration, or issues with chunked encoding.

- RESULT_CANT_CONNECT = 2
  Request failed while connecting.

- RESULT_CANT_RESOLVE = 3
  Request failed while resolving.

- RESULT_CONNECTION_ERROR = 4
  Request failed due to connection (read/write) error.

- RESULT_TLS_HANDSHAKE_ERROR = 5
  Request failed on TLS handshake.

- RESULT_NO_RESPONSE = 6
  Request does not have a response (yet).

- RESULT_BODY_SIZE_LIMIT_EXCEEDED = 7
  Request exceeded its maximum size limit, see body_size_limit.

- RESULT_BODY_DECOMPRESS_FAILED = 8
  Request failed due to an error while decompressing the response body. Possible causes include unsupported or incorrect compression format, corrupted data, or incomplete transfer.

- RESULT_REQUEST_FAILED = 9
  Request failed (currently unused).

- RESULT_DOWNLOAD_FILE_CANT_OPEN = 10
  HTTPRequest couldn't open the download file.

- RESULT_DOWNLOAD_FILE_WRITE_ERROR = 11
  HTTPRequest couldn't write to the download file.

- RESULT_REDIRECT_LIMIT_REACHED = 12
  Request reached its maximum redirect limit, see max_redirects.

- RESULT_TIMEOUT = 13
  Request failed due to a timeout. If you expect requests to take a long time, try increasing the value of timeout or setting it to 0.0 to remove the timeout completely.
