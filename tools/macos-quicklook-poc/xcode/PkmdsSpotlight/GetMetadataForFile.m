#import <CoreFoundation/CoreFoundation.h>
#import <CoreServices/CoreServices.h>

// MDImporter CFPlugin boilerplate.  The sole job of this importer is to assert
// com.bondcodes.pkmds.save-file as the content type for .sav/.dat/.fla files so
// that Finder dispatches our Quick Look extension rather than treating them as
// opaque data.  No PKHeX parsing is needed here.

typedef struct {
    MDImporterInterfaceStruct **interface;
} PkmdsSpotlightPlugin;

static HRESULT QueryInterface(void *self, REFIID iid, LPVOID *ppv) {
    CFUUIDRef importerIID = CFUUIDCreateFromString(NULL, CFSTR("6B99B0B0-27B1-11D8-9DF4-000A95B0B976"));
    CFUUIDRef requested   = CFUUIDCreateFromUUIDBytes(NULL, iid);
    HRESULT hr = E_NOINTERFACE;
    if (CFEqual(requested, importerIID)) {
        *ppv = self;
        hr = S_OK;
    }
    CFRelease(requested);
    CFRelease(importerIID);
    return hr;
}

static ULONG AddRef(void *self)  { (void)self; return 1; }
static ULONG Release(void *self) { (void)self; return 1; }

static Boolean GetMetadataForFile(void *self,
                                   CFMutableDictionaryRef attrs,
                                   CFStringRef contentTypeUTI,
                                   CFStringRef pathToFile) {
    (void)self;
    (void)contentTypeUTI;
    (void)pathToFile;
    CFDictionarySetValue(attrs, kMDItemContentType,
                         CFSTR("com.bondcodes.pkmds.save-file"));
    return true;
}

static MDImporterInterfaceStruct  gPluginInterface = {
    NULL,
    QueryInterface,
    AddRef,
    Release,
    GetMetadataForFile,
};
// COM-style double indirection: the plugin struct holds a pointer to a pointer.
static MDImporterInterfaceStruct *gPluginInterfacePtr = &gPluginInterface;
static PkmdsSpotlightPlugin gPlugin = { &gPluginInterfacePtr };

// Entry point declared in Info.plist CFPlugInFactories.
void *MetadataImporterPluginFactory(CFAllocatorRef allocator, CFUUIDRef typeID) {
    (void)allocator;
    CFUUIDRef importerTypeID = CFUUIDCreateFromString(NULL, CFSTR("8B08C4BF-415B-11D8-B3F9-0003936726FC"));
    void *result = NULL;
    if (CFEqual(typeID, importerTypeID)) {
        result = &gPlugin;
    }
    CFRelease(importerTypeID);
    return result;
}
