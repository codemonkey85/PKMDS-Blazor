window.showSaveFilePickerAndWrite = async function (fileName, byteArray) {
    try {
        const opts = {
            suggestedName: fileName,
            types: [{
                description: 'Save File',
                accept: { 'application/octet-stream': ['.sav'] }
            }]
        };

        const handle = await window.showSaveFilePicker(opts);
        const writable = await handle.createWritable();
        await writable.write(new Blob([new Uint8Array(byteArray)], { type: "application/octet-stream" }));
        await writable.close();
    } catch (ex) {
        console.error(ex);
        throw ex;
    }
};
