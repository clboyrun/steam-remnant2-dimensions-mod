#include "GunfireOnlineTitleStorage.h"

UGunfireOnlineTitleStorage::UGunfireOnlineTitleStorage() {
}

void UGunfireOnlineTitleStorage::OnReadFileProgress(const FString& Filename, uint64 NumBytes) {
}

void UGunfireOnlineTitleStorage::OnReadFileComplete(bool bWasSuccessful, const FString& Filename) {
}

void UGunfireOnlineTitleStorage::OnEnumerateFilesComplete(bool bWasSuccessful, const FString& Error) {
}

bool UGunfireOnlineTitleStorage::IsBusy() const {
    return false;
}

UGunfireOnlineTitleStorage* UGunfireOnlineTitleStorage::GetInstance(const UObject* WorldContextObject) {
    return NULL;
}

TArray<UTitleStorageFile*> UGunfireOnlineTitleStorage::GetFileManifest() {
    return TArray<UTitleStorageFile*>();
}

UTitleStorageFile* UGunfireOnlineTitleStorage::GetFile(const FString& Filename) {
    return NULL;
}

bool UGunfireOnlineTitleStorage::DownloadFileManifest() {
    return false;
}

bool UGunfireOnlineTitleStorage::DownloadFile(const FString& Filename) {
    return false;
}


