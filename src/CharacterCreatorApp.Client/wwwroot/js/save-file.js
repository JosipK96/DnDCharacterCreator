// Saves bytes from .NET as a file. The API wants the user's token, so a plain link to the PDF
// would not do: the client fetches it, then hands it to the browser here.
window.saveFile = (fileName, contentType, bytes) => {
    const url = URL.createObjectURL(new Blob([bytes], { type: contentType }));
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
};
