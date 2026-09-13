# Printable Wall Art Production Pipeline

> **Ownership note:** This document is maintained with `PhotoshopAutomation.Api` because it describes the cross-repository printable wall-art production workflow. `Mosswick.World` is the upstream creative source that produces artwork and the production manifest; the Mockup Workflow Platform owns downstream batch processing, Photoshop orchestration, asset transfer, and workflow completion.

## Purpose

This document explains the complete printable wall-art workflow from source artwork through final mockup storage.

Use it as a reference when:

- debugging a failed batch
- adding a new product type
- changing the Photoshop template
- updating an API
- explaining the project in a GitHub portfolio
- returning to the project after time away

---

## High-Level Pipeline

```text
Mosswick.World
    |
    v
Generate mockup-manifest.json
    |
    v
MockupWorkflow Admin
    |
    v
Create batch records and folder structure
    |
    v
Photoshop UXP Plugin
    |
    v
Workflow Dispatcher
    |
    v
Printable Wall Art Processor
    |
    v
Download source artwork through PNG API
    |
    v
Duplicate Photoshop template
    |
    v
Replace Smart Object contents
    |
    v
Export final PNG locally
    |
    v
Upload final PNG through PNG API
    |
    v
Store file in Docker-managed /data/builds volume
    |
    v
Mark record complete
    |
    v
Refresh pending batch queue
```

---

## Pipeline Components

### 1. Mosswick.World

**Responsibility:** Owns the original artwork and its creative metadata.

Mosswick produces:

- source artwork
- artwork identifiers
- generation identifiers
- product type
- approval status
- `mockup-manifest.json`

Mosswick remains the authoritative owner of the original artwork.

It does not own the production copies used by the mockup workflow.

---

### 2. Mockup Manifest

The manifest is the handoff contract between Mosswick and the production platform.

Typical fields include:

```text
ArtworkId
GenerationId
SourceImageFile
ProductType
Status
```

For printable wall art:

```text
ProductType = printable-wall-art
```

The manifest tells the Admin application what artwork should enter production.

---

### 3. MockupWorkflow Admin

**Responsibility:** Imports the manifest and creates a production batch.

The Admin application:

1. reads the manifest
2. creates a batch ID
3. converts manifest records into workflow records
4. creates input and output folder records
5. copies or uploads the source artwork into the batch
6. exposes the batch to the Photoshop plugin

Example batch ID:

```text
1784986020513
```

---

### 4. Docker-Managed Build Storage

The shared production root is:

```text
/data/builds
```

Each batch follows this structure:

```text
/data/builds/
    <batchId>/
        <productType>/
            input_folders/
                <folderName>/
                    <source image>
            mockup_folders/
                <folderName>/
                    <generated output>
```

Printable wall-art example:

```text
/data/builds/
    1784986020513/
        printable-wall-art/
            input_folders/
                artwork-lantern_bakery-001/
                    Artboard 2.png
            mockup_folders/
                artwork-lantern_bakery-001/
                    Artboard 2-printable-wall-art.png
```

The Docker volume is the shared production source of truth.

A successful local Photoshop export is not enough. The final PNG must also be uploaded into this storage.

---

### 5. Records API

**Responsibility:** Stores workflow records and batch status.

The Photoshop plugin uses the Records API to:

- load pending batches
- load records for a selected batch
- determine product type
- mark individual records complete

A record should only be marked complete after the final PNG has been uploaded successfully.

---

### 6. PSD Template Records

PSD templates are stored as records with fields such as:

```text
ProductType
FilePathName
Description
WorkflowStep
```

Printable wall-art example:

```text
ProductType: printable-wall-art
WorkflowStep: 10
FilePathName: printable-wall-art/printable-wall-art-10.psd
```

The plugin:

1. loads PSD records
2. filters them by product type
3. orders them by workflow step
4. opens the correct template

---

### 7. Photoshop UXP Plugin

**Responsibility:** Coordinates the Photoshop production process.

The plugin:

1. loads pending batches
2. lets the user select a batch
3. determines its product type
4. loads matching PSD workflow records
5. opens the required template
6. dispatches each workflow step
7. refreshes the queue after processing

The plugin should remain generic and not contain Mosswick-specific business logic.

---

### 8. Workflow Dispatcher

The dispatcher routes work to the correct processor.

Conceptually:

```text
product type + workflow step
    |
    v
registered processor
```

For printable wall art:

```text
ProductType: printable-wall-art
WorkflowStep: 10
Processor: printable-wall-art
```

The dispatcher handles routing.

The processor handles product-specific Photoshop behavior.

---

### 9. Printable Wall Art Processor

The processor performs the actual Photoshop workflow for each record.

Current sequence:

```text
Validate record
    |
    v
Download source PNG
    |
    v
Create or obtain UXP file entry
    |
    v
Duplicate template document
    |
    v
Find Smart Object layer named "artwork"
    |
    v
Replace Smart Object contents
    |
    v
Export PNG to local UXP output folder
    |
    v
Upload PNG through PNG API
    |
    v
Close working document without saving PSD changes
    |
    v
Mark workflow record complete
```

The original PSD template stays unchanged because the processor works on a duplicated document.

---

## Smart Object Replacement

The printable wall-art template contains a Smart Object layer named:

```text
artwork
```

The processor:

1. searches the document layer tree
2. locates the layer by name
3. selects it
4. calls Photoshop's placed-layer replacement command
5. replaces the Smart Object contents with the downloaded PNG

A missing or renamed Smart Object layer should stop the item and leave the record pending.

---

## Export Behavior

The generated output name currently follows this pattern:

```text
<source-name>-printable-wall-art.png
```

Example:

```text
Artboard 2-printable-wall-art.png
```

The exported file is first created as a UXP-accessible local file.

That local export must then be uploaded to the PNG API.

---

## PNG API Upload

The upload helper:

1. reads the local exported PNG as binary data
2. builds the remote production path
3. URL-encodes the folder name and filename
4. sends the PNG with an HTTP `POST`
5. verifies that the response succeeded

Example remote path:

```text
1784986020513/
printable-wall-art/
mockup_folders/
artwork-lantern_bakery-001/
Artboard 2-printable-wall-art.png
```

Filename spaces are valid because the path segments are URL-encoded.

The earlier missing-file issue was not caused by spaces. It occurred because the PNG had been exported locally but had not yet been uploaded into Docker-managed storage.

---

## Completion Rule

A record must not be marked complete until all of these steps succeed:

```text
Smart Object replacement
Export
Upload
```

Correct order:

```text
Replace
    |
    v
Export
    |
    v
Upload
    |
    v
Mark complete
```

Incorrect order:

```text
Export
    |
    v
Mark complete
    |
    v
Upload later
```

The completion rule prevents the Admin interface from reporting success when the final file is missing.

---

## Failure Behavior

If an item fails:

- increment the failed count
- log the item number and error
- close the duplicate Photoshop document
- do not save changes to the template
- do not mark the record complete
- leave the item available for another attempt

Examples of valid failure conditions:

```text
source file missing
invalid UXP file entry
Smart Object layer not found
Photoshop replacement failed
PNG export failed
PNG upload failed
API completion update failed
```

---

## Confirmed End-to-End Test

The following behavior has been verified:

```text
Source PNG downloaded successfully
Smart Object layer replaced successfully
Final PNG exported successfully
Final PNG uploaded through the PNG API
Final PNG appeared under /data/builds
Record marked complete
Pending batch count decreased
```

Verified output:

```text
/data/builds/1784986020513/
printable-wall-art/
mockup_folders/
artwork-lantern_bakery-001/
Artboard 2-printable-wall-art.png
```

---

## Debugging Checklist

When a printable-wall-art batch fails, check the pipeline in this order.

### Batch and record

```text
Is the batch still pending?
Did the plugin load the correct record?
Is ProductType printable-wall-art?
Does the record have Id, FolderName, Filename, and InputFolderPath?
```

### Source artwork

```text
Does the source file exist under input_folders?
Can the PNG API serve it?
Do its first bytes match the PNG signature?
```

PNG signature:

```text
137, 80, 78, 71, 13, 10, 26, 10
```

### Template

```text
Was the correct PSD opened?
Did the template duplicate successfully?
Does the duplicate contain the "artwork" Smart Object?
```

### Replacement

```text
Is the downloaded value a valid UXP File entry?
Did placedLayerReplaceContents run successfully?
```

### Export

```text
Was the final PNG created locally?
Does the filename match the expected pattern?
```

### Upload

```text
Did the upload helper run?
Was the remote path correct?
Did the PNG API return a successful status?
Does the file appear under /data/builds?
```

### Completion

```text
Was markMockupComplete called only after upload?
Did the pending queue decrease?
Does the Admin interface show the record as processed?
```

---

## Responsibility Boundaries

### Mosswick.World owns

```text
creative artwork
creative metadata
generation records
approval state
manifest generation
```

### MockupWorkflow owns

```text
production batches
workflow records
temporary production copies
folder structure
processing state
```

### Photoshop UXP Plugin owns

```text
Photoshop orchestration
template opening
processor dispatch
image replacement
local export
upload initiation
completion calls
```

### PNG API owns

```text
binary file transfer
reading source images
writing generated outputs
access to Docker-managed build storage
```

### Docker storage owns

```text
shared production files
batch input folders
batch mockup folders
```

---

## How New Product Types Fit In

The core platform does not need to be redesigned for every product type.

A new product type primarily requires:

1. one or more PSD template records
2. workflow step numbers
3. a registered processor
4. product-specific Photoshop behavior
5. output naming and upload rules

Possible future processors:

```text
tshirt
hoodie
mug
canvas
framed-print
metal-print
phone-case
```

All can reuse the same general pipeline:

```text
Manifest
→ Batch
→ Dispatcher
→ Processor
→ Export
→ Upload
→ Complete
```

---

## Current Milestone

The printable-wall-art processor is no longer just an architectural proof.

It has completed a real end-to-end production run.

The platform has now demonstrated that it can:

- accept artwork from an external creative project
- create and manage production batches
- dispatch Photoshop workflows by product type
- generate final output files
- store those outputs in shared Docker-managed storage
- update processing status only after confirmed success

This is the foundation for additional product processors.
