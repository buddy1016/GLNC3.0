/*
 Navicat MySQL Data Transfer

 Source Server         : .101 2ts_glnc
 Source Server Type    : MariaDB
 Source Server Version : 100528 (10.5.28-MariaDB-0+deb11u1)
 Source Host           : 172.16.75.101:3306
 Source Schema         : glnc_2ts

 Target Server Type    : MariaDB
 Target Server Version : 100528 (10.5.28-MariaDB-0+deb11u1)
 File Encoding         : 65001

 Date: 18/12/2025 22:28:08
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for api_key
-- ----------------------------
DROP TABLE IF EXISTS `api_key`;
CREATE TABLE `api_key`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `api_key` char(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 3 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;



-- ----------------------------
-- Table structure for users
-- ----------------------------
DROP TABLE IF EXISTS `users`;
CREATE TABLE `users`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` char(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `password` char(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `role` int(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 10 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of users
-- ----------------------------
INSERT INTO `users` VALUES (2, 'panda', '2f1585c73d6fb92a767e02196e0e95fe032bef723a1da2c94cfa6d7ecb5817df', 1);
INSERT INTO `users` VALUES (4, 'fano ADMIN', '0605a98bc2eefcc0690a2aedc57fd029736c44eea456bff38f4d6c32fe4a4626', 2);
INSERT INTO `users` VALUES (5, 'marc', '8064fcf9b026fc70c571184365e8848272d5eb6fddd592a53c82176d5e9fafea', 1);
INSERT INTO `users` VALUES (6, 'albert', '8a9f163bbce85168eed0f9bb4158bde0662aa436012fd61595e02d89dee97a94', 1);
INSERT INTO `users` VALUES (7, 'yoiris', 'd0b91998784c48db60d8ad19947bfa99069ef8c99d03d7d951a8054f0322e948', 1);
INSERT INTO `users` VALUES (8, 'Christophe', '836e921645eeb9b8a052c94bbb50c56db8159806a4d7ccf03d8b09200f003395', 1);
INSERT INTO `users` VALUES (9, 'fano', '9baecdec4625f4c8ad867b38ffb1828937402ecf2b9b93558fe5f71f59d01305', 1);


-- ----------------------------
-- Table structure for attendance_tracking
-- ----------------------------
DROP TABLE IF EXISTS `attendance_tracking`;
CREATE TABLE `attendance_tracking`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `time` datetime NOT NULL,
  `lati` double NOT NULL,
  `longi` double NOT NULL,
  `alti` double NOT NULL,
  `type` tinyint(1) NOT NULL,
  `user_id` int(11) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `user_id`(`user_id`) USING BTREE,
  CONSTRAINT `attendance_tracking_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 59 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of attendance_tracking
-- ---------------------------

-- ----------------------------
-- Table structure for supplier
-- ----------------------------
DROP TABLE IF EXISTS `supplier`;
CREATE TABLE `supplier`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `supplier_name` char(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `mail` char(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `check` tinyint(1) NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 16 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of supplier
-- ----------------------------
INSERT INTO `supplier` VALUES (3, 'SODEVIA', 'resp.logistic.2ts@gmail.com', 1);
INSERT INTO `supplier` VALUES (4, 'OCEF', 'resp.logistic.2ts@gmail.com', 0);
INSERT INTO `supplier` VALUES (5, 'FRIGODOM', 'resp.logistic.2ts@gmail.com', 0);
INSERT INTO `supplier` VALUES (6, 'COTES DASIE', 'resp.logistic.2ts@gmail.com', 0);
INSERT INTO `supplier` VALUES (7, 'AUCHAN AUTEUIL', 'resp.logistic.2ts@gmail.com', 0);
INSERT INTO `supplier` VALUES (8, 'CORAIL BAKO', 'resp.logistic.2ts@gmail.com', 0);
INSERT INTO `supplier` VALUES (9, 'KORAL KOUMAC', 'resp.logistic.2ts@gmail.com', 0);
INSERT INTO `supplier` VALUES (10, 'FAIR BRANT', 'resp.logistic.2ts@gmail.com', 0);
INSERT INTO `supplier` VALUES (11, 'AUCHAN QUAI FERRY', 'resp.logistic.2ts@gmail.com', 0);
INSERT INTO `supplier` VALUES (12, 'OCEF POMME DE TERRE', 'resp.logistic.2ts@gmail.com', 0);
INSERT INTO `supplier` VALUES (13, 'BOUCHERIE DE PAITA', 'resp.logistic.2ts@gmail.com', 0);
INSERT INTO `supplier` VALUES (14, 'BOUCHERIE DE LA PLACE', 'resp.logistic.2ts@gmail.com', 0);
INSERT INTO `supplier` VALUES (15, 'BURGER KING', 'resp.logistic.2ts@gmail.com', 0);

-- ----------------------------
-- Table structure for trucks
-- ----------------------------
DROP TABLE IF EXISTS `trucks`;
CREATE TABLE `trucks`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `license` char(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `brand` char(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `model` char(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `color` char(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 9 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of trucks
-- ----------------------------
INSERT INTO `trucks` VALUES (5, '418176', 'IVECO', '3,5T', '#ffffff');
INSERT INTO `trucks` VALUES (6, '415935', 'RENAULT MAXCITY', '5T', '#ffffff');
INSERT INTO `trucks` VALUES (7, '472479', 'RENAULT MASTER', '5T', '#ffffff');
INSERT INTO `trucks` VALUES (8, '454633', 'RENAULT MASTER', '5T', '#ffffff');



-- ----------------------------
-- Table structure for delivery
-- ----------------------------
DROP TABLE IF EXISTS `delivery`;
CREATE TABLE `delivery`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `date_time_appointment` datetime NOT NULL,
  `date_time_leave` datetime NOT NULL,
  `date_time_accept` datetime NULL DEFAULT NULL,
  `date_time_arrival` datetime NULL DEFAULT NULL,
  `description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `sign_client` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `satisfaction_client` int(11) NULL DEFAULT NULL,
  `return_flag` tinyint(1) NOT NULL DEFAULT 0,
  `truck_id` int(11) NOT NULL,
  `client` char(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `address` char(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `contacts` char(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `invoice` char(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `supplier_id` int(11) NOT NULL,
  `weight` double NOT NULL,
  `comment` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `invoice_image` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `supplier_id`(`supplier_id`) USING BTREE,
  INDEX `user_id`(`user_id`) USING BTREE,
  INDEX `truck_id`(`truck_id`) USING BTREE,
  CONSTRAINT `delivery_ibfk_1` FOREIGN KEY (`supplier_id`) REFERENCES `supplier` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `delivery_ibfk_2` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `delivery_ibfk_3` FOREIGN KEY (`truck_id`) REFERENCES `trucks` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 36 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for delivery_geolocation
-- ----------------------------
DROP TABLE IF EXISTS `delivery_geolocation`;
CREATE TABLE `delivery_geolocation`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `sign_lati` double NOT NULL,
  `sign_longi` double NOT NULL,
  `sign_alti` double NOT NULL,
  `delivery_id` int(11) NOT NULL,
  `user_id` int(11) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `delivery_id`(`delivery_id`) USING BTREE,
  INDEX `user_id`(`user_id`) USING BTREE,
  CONSTRAINT `delivery_geolocation_ibfk_1` FOREIGN KEY (`delivery_id`) REFERENCES `delivery` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `delivery_geolocation_ibfk_2` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 10 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for drivergeolocation
-- ----------------------------
DROP TABLE IF EXISTS `drivergeolocation`;
CREATE TABLE `drivergeolocation`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `lati` double NOT NULL,
  `longi` double NOT NULL,
  `alti` double NOT NULL,
  `date_time` datetime NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 160 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for message
-- ----------------------------
DROP TABLE IF EXISTS `message`;
CREATE TABLE `message`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `sender_id` int(11) NOT NULL,
  `receive_id` int(11) NOT NULL,
  `message` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `sendtime` datetime NOT NULL,
  `receivetime` datetime NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `sender_id`(`sender_id`) USING BTREE,
  INDEX `receive_id`(`receive_id`) USING BTREE,
  CONSTRAINT `message_ibfk_1` FOREIGN KEY (`sender_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `message_ibfk_2` FOREIGN KEY (`receive_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of message
-- ----------------------------


SET FOREIGN_KEY_CHECKS = 1;
