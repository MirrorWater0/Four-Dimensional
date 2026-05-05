# X509Certificate

## Meta

- Name: X509Certificate
- Source: X509Certificate.xml
- Inherits: Resource
- Inheritance Chain: X509Certificate -> Resource -> RefCounted -> Object

## Brief Description

An X509 certificate (e.g. for TLS).

## Description

The X509Certificate class represents an X509 certificate. Certificates can be loaded and saved like any other Resource. They can be used as the server certificate in StreamPeerTLS.accept_stream() (along with the proper CryptoKey), and to specify the only certificate that should be accepted when connecting to a TLS server via StreamPeerTLS.connect_to_stream().

## Quick Reference

```
[methods]
load(path: String) -> int (Error)
load_from_string(string: String) -> int (Error)
save(path: String) -> int (Error)
save_to_string() -> String
```

## Tutorials

- [SSL certificates]($DOCS_URL/tutorials/networking/ssl_certificates.html)

## Methods

- load(path: String) -> int (Error)
  Loads a certificate from path ("*.crt" file).

- load_from_string(string: String) -> int (Error)
  Loads a certificate from the given string.

- save(path: String) -> int (Error)
  Saves a certificate to the given path (should be a "*.crt" file).

- save_to_string() -> String
  Returns a string representation of the certificate, or an empty string if the certificate is invalid.
