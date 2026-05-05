# CryptoKey

## Meta

- Name: CryptoKey
- Source: CryptoKey.xml
- Inherits: Resource
- Inheritance Chain: CryptoKey -> Resource -> RefCounted -> Object

## Brief Description

A cryptographic key (RSA or elliptic-curve).

## Description

The CryptoKey class represents a cryptographic key. Keys can be loaded and saved like any other Resource. They can be used to generate a self-signed X509Certificate via Crypto.generate_self_signed_certificate() and as private key in StreamPeerTLS.accept_stream() along with the appropriate certificate.

## Quick Reference

```
[methods]
is_public_only() -> bool [const]
load(path: String, public_only: bool = false) -> int (Error)
load_from_string(string_key: String, public_only: bool = false) -> int (Error)
save(path: String, public_only: bool = false) -> int (Error)
save_to_string(public_only: bool = false) -> String
```

## Tutorials

- [SSL certificates]($DOCS_URL/tutorials/networking/ssl_certificates.html)

## Methods

- is_public_only() -> bool [const]
  Returns true if this CryptoKey only has the public part, and not the private one.

- load(path: String, public_only: bool = false) -> int (Error)
  Loads a key from path. If public_only is true, only the public key will be loaded. **Note:** path should be a "*.pub" file if public_only is true, a "*.key" file otherwise.

- load_from_string(string_key: String, public_only: bool = false) -> int (Error)
  Loads a key from the given string_key. If public_only is true, only the public key will be loaded.

- save(path: String, public_only: bool = false) -> int (Error)
  Saves a key to the given path. If public_only is true, only the public key will be saved. **Note:** path should be a "*.pub" file if public_only is true, a "*.key" file otherwise.

- save_to_string(public_only: bool = false) -> String
  Returns a string containing the key in PEM format. If public_only is true, only the public key will be included.
