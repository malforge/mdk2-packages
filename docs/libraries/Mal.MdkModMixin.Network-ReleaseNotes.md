# Release Notes - Mal.MdkModMixin.Network

````````text
- 1.0.1
  Channel and message ids are the same on every machine. They came from the runtime's string hash, which on
  .NET 10 differs in every process, so a .NET 10 game and a server never understood each other. Every id changes:
  every machine in a world needs this version.
- 1.0.0
  First release

````````

---

*Release notes auto-generated from `_releasenotes` file. Last updated: 2026-11-25*
