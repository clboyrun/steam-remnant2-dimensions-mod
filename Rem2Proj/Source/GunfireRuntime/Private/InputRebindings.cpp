#include "InputRebindings.h"

UInputRebindings::UInputRebindings() {
    this->ActionRebindings.AddDefaulted(35);
    this->AxisRebindings.AddDefaulted(5);
}

void UInputRebindings::ResetInputRebindings() {
}

void UInputRebindings::RemoveEnhancedActionRebinding(const FEnhancedActionKeyMapping& TemplateMapping) {
}

void UInputRebindings::RemoveAxisRebinding(const FInputAxisKeyMapping& Axis) {
}

void UInputRebindings::RemoveActionRebinding(const FInputActionKeyMapping& Action) {
}

void UInputRebindings::AddEnhancedActionRebinding(const FEnhancedActionKeyMapping& TemplateMapping) {
}

void UInputRebindings::AddAxisRebinding(const FInputAxisKeyMapping& Axis) {
}

void UInputRebindings::AddActionRebinding(const FInputActionKeyMapping& Action) {
}


