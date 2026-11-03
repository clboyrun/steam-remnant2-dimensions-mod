#include "TitleStorageFileHeader.h"

FTitleStorageFileHeader::FTitleStorageFileHeader() {
    this->FileSize = 0;
    this->DownloadSize = 0;
    this->LocalFileState = ETitleStorageLocalFileState::HeaderOnly;
}

