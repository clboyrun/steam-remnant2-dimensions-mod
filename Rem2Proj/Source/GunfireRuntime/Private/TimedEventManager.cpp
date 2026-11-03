#include "TimedEventManager.h"

UTimedEventManager::UTimedEventManager() {
    this->VariableComponent = NULL;
}

void UTimedEventManager::OnRootVariableContextCreated(URootVariableContext* Context) {
}

void UTimedEventManager::OnFileManifestDownloaded(bool bSuccess, const TArray<UTitleStorageFile*>& FileManifest) {
}

void UTimedEventManager::OnFileDownloaded(bool bSuccess, UTitleStorageFile* File) {
}

bool UTimedEventManager::HasTags(const TArray<FName>& RequiredTags) const {
    return false;
}

bool UTimedEventManager::HasEvent(FName EventName) const {
    return false;
}

TArray<FName> UTimedEventManager::GetTags() const {
    return TArray<FName>();
}

TArray<FTimedEvent> UTimedEventManager::GetEvents() const {
    return TArray<FTimedEvent>();
}

bool UTimedEventManager::GetEvent(FName EventName, FTimedEvent& OutEvent) const {
    return false;
}


