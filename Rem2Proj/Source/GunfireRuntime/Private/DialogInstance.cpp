#include "DialogInstance.h"

UDialogInstance::UDialogInstance() {
    this->ID = 0;
    this->DialogManager = NULL;
}

void UDialogInstance::OnSubtitleComplete(USubtitleInstance* Subtitle) {
}

void UDialogInstance::OnSubtitleAdded(USubtitleInstance* Subtitle) {
}

bool UDialogInstance::IsDialogSource(AActor* Source) {
    return false;
}

void UDialogInstance::ForceComplete() {
}

void UDialogInstance::Complete() {
}


