terraform {
  backend "azurerm" {}

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">=3.100.0"
    }
    random = {
      source  = "hashicorp/random"
      version = ">=3.6.0"
    }
    null = {
      source  = "hashicorp/null"
      version = ">=3.2.0"
    }
  }
  required_version = ">=1.3.0"
}

provider "azurerm" {
  features {}
}