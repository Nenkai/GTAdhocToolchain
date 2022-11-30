# GTAdhocToolchain.Packaging

Handles packaged script projects `.mpackage`. Allows unpacking and packing of such contaners. Seen used in GT6.

Packaged projects contain the entirety of code required for a project to run, with each source file/root/mproject being seperated within the package to avoid loading the entirety of a project and reduce the size as each file within the container is ZLib compressed.
