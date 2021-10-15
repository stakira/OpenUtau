//
// Created by SeleDreams on 15/10/2021.
//

#include <string>
#include <process.hpp>
#include <iostream>
#include "ui/OpenUTAUUI.h"

OpenUTAUUI::OpenUTAUUI() {
    const uint minWidth = 400;
    const uint minHeight = 200;
    setGeometryConstraints(minWidth,minHeight,true,true);

    const float width = getWidth();
    const float height = getHeight();

}

void OpenUTAUUI::uiIdle() {
    UI::uiIdle();
    repaint();
}


void OpenUTAUUI::onDisplay() {

}

float OpenUTAUUI::getParameterValue(uint32_t index) {
    return 0;
}

void OpenUTAUUI::parameterChanged(uint32_t, float value) {

}
